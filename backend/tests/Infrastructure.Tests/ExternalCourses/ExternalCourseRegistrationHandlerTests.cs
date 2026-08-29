using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using StudyOrganizer.Application.ExternalCourses;
using StudyOrganizer.Domain.ExternalCourses;
using StudyOrganizer.Domain.Modules;
using StudyOrganizer.Infrastructure.ExternalCourses;

namespace StudyOrganizer.Infrastructure.Tests.ExternalCourses;

public sealed class ExternalCourseRegistrationHandlerTests
{
    [Fact]
    public async Task RegisterAsync_AfterSuccessfulScan_MaterializesRelevantSnapshotWithoutFetch()
    {
        await using var setup = await ExternalCourseScenario.CreateAsync(subscriberCount: 1);
        setup.Provider.SetSnapshot(ExternalCourseSnapshots.Initial);
        await setup.Handler.ScanAsync(setup.OwnerIds[0], setup.SubscriptionIds[0]);
        var fetchesBeforeRegistration = setup.Provider.FetchCount;
        var secondOwner = await setup.Database.CreateUserAsync("second@example.com");

        var result = await setup.RegistrationHandler.RegisterAsync(
            secondOwner,
            "https://mock-moodle.local/courses/software-engineering-2026");

        Assert.Equal(CourseRegistrationOutcome.Created, result.Outcome);
        Assert.Equal(fetchesBeforeRegistration, setup.Provider.FetchCount);
        var tasks = await setup.TasksForAsync(secondOwner);
        var task = Assert.Single(tasks);
        Assert.Equal("Exercise 1", task.Title);
        Assert.Single(await setup.Database.Context.ExternalTaskLinks
            .Where(link => link.CourseSubscriptionId == result.Subscription!.Id)
            .ToListAsync());
    }

    [Fact]
    public async Task RegisterAsync_AfterSuccessfulScan_MaterializesOnlyVisibleFutureEligibleContents()
    {
        await using var setup = await ExternalCourseScenario.CreateAsync(subscriberCount: 1);
        setup.Provider.SetSnapshot(new CourseSnapshot(
            "mock-moodle",
            "software-engineering-2026",
            true,
            [
                ExternalCourseSnapshots.Initial.Contents[0],
                ExternalCourseSnapshots.Initial.Contents[0] with
                {
                    ProviderContentId = "past-exercise",
                    Title = "Past exercise",
                    StructuredDueDateUtc = setup.Database.Now.AddDays(-1)
                },
                ExternalCourseSnapshots.Initial.Contents[1]
            ]));
        await setup.Handler.ScanAsync(setup.OwnerIds[0], setup.SubscriptionIds[0]);
        setup.Provider.SetSnapshot(ExternalCourseSnapshots.WithoutExerciseOne);
        await setup.Handler.ScanAsync(setup.OwnerIds[0], setup.SubscriptionIds[0]);
        var secondOwner = await setup.Database.CreateUserAsync("late@example.com");

        var result = await setup.RegistrationHandler.RegisterAsync(
            secondOwner,
            "https://mock-moodle.local/courses/software-engineering-2026");

        Assert.Equal(CourseRegistrationOutcome.Created, result.Outcome);
        Assert.Empty(await setup.TasksForAsync(secondOwner));
        Assert.Empty(await setup.Database.Context.ExternalTaskLinks
            .Where(link => link.CourseSubscriptionId == result.Subscription!.Id)
            .ToListAsync());
    }

    [Fact]
    public async Task RegisterAsync_LateTaskPersistenceFails_RollsBackSubscriptionModuleAndTasks()
    {
        await using var setup = await ExternalCourseScenario.CreateScannedAsync(
            subscriberCount: 1);
        var secondOwner = await setup.Database.CreateUserAsync("rollback@example.com");
        await setup.Database.Context.Database.ExecuteSqlRawAsync(
            """
            CREATE TRIGGER fail_late_task_insert
            BEFORE INSERT ON tasks
            BEGIN
                SELECT RAISE(ABORT, 'forced late task persistence failure');
            END;
            """);

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            setup.RegistrationHandler.RegisterAsync(
                secondOwner,
                "https://mock-moodle.local/courses/software-engineering-2026"));

        setup.Database.Context.ChangeTracker.Clear();
        Assert.Single(await setup.Database.Context.CourseSubscriptions.ToListAsync());
        Assert.Single(await setup.Database.Context.Modules.ToListAsync());
        Assert.Single(await setup.Database.Context.Tasks.ToListAsync());
        Assert.Single(await setup.Database.Context.ExternalTaskLinks.ToListAsync());
        Assert.DoesNotContain(
            await setup.Database.Context.Modules.ToListAsync(),
            module => module.OwnerId == secondOwner);
    }

    [Fact]
    public async Task RegisterAsync_TwoAliases_CreateOneSharedCourseAndOneSubscription()
    {
        await using var database = await ExternalCourseTestDatabase.CreateAsync();
        var ownerId = await database.CreateUserAsync("student@example.com");
        var provider = ControlledExternalCourseProvider.ForSoftwareEngineering();
        var handler = CreateHandler(database, provider);

        var first = await handler.RegisterAsync(
            ownerId,
            "https://mock-moodle.local/courses/software-engineering-2026");
        var second = await handler.RegisterAsync(
            ownerId,
            "https://mock-moodle.local/course/view.php?id=se-2026");

        Assert.Equal(CourseRegistrationOutcome.Created, first.Outcome);
        Assert.Equal(CourseRegistrationOutcome.Existing, second.Outcome);
        Assert.Equal(first.Subscription!.Id, second.Subscription!.Id);
        Assert.Equal("Software Engineering", first.Subscription.CourseName);
        Assert.Equal("NeverScanned", first.Subscription.LastScanStatus);
        Assert.Single(database.Context.ExternalCourses);
        Assert.Single(database.Context.CourseSubscriptions);
        Assert.Single(database.Context.Modules);
    }

    [Theory]
    [InlineData("not a URI")]
    [InlineData("/courses/software-engineering-2026")]
    [InlineData("http://mock-moodle.local/courses/software-engineering-2026")]
    public async Task RegisterAsync_InvalidAbsoluteHttpsUri_ReturnsInvalidUrl(string courseUrl)
    {
        await using var database = await ExternalCourseTestDatabase.CreateAsync();
        var ownerId = await database.CreateUserAsync("student@example.com");
        var handler = CreateHandler(
            database,
            ControlledExternalCourseProvider.ForSoftwareEngineering());

        var result = await handler.RegisterAsync(ownerId, courseUrl);

        Assert.Equal(CourseRegistrationOutcome.InvalidUrl, result.Outcome);
        Assert.Null(result.Subscription);
        Assert.Empty(database.Context.ExternalCourses);
        Assert.Empty(database.Context.CourseSubscriptions);
        Assert.Empty(database.Context.Modules);
    }

    [Theory]
    [InlineData("https://example.com/courses/software-engineering-2026")]
    [InlineData("https://mock-moodle.local/courses/other")]
    [InlineData("https://mock-moodle.local/courses/software-engineering-2026/")]
    public async Task RegisterAsync_UnsupportedHostOrPath_ReturnsUnsupportedUrl(string courseUrl)
    {
        await using var database = await ExternalCourseTestDatabase.CreateAsync();
        var ownerId = await database.CreateUserAsync("student@example.com");
        var handler = CreateHandler(
            database,
            ControlledExternalCourseProvider.ForSoftwareEngineering());

        var result = await handler.RegisterAsync(ownerId, courseUrl);

        Assert.Equal(CourseRegistrationOutcome.UnsupportedUrl, result.Outcome);
        Assert.Null(result.Subscription);
        Assert.Empty(database.Context.ExternalCourses);
        Assert.Empty(database.Context.CourseSubscriptions);
        Assert.Empty(database.Context.Modules);
    }

    [Fact]
    public async Task RegisterAsync_TwoOwners_ShareCourseAndReceiveSeparateModules()
    {
        await using var database = await ExternalCourseTestDatabase.CreateAsync();
        var firstOwnerId = await database.CreateUserAsync("first@example.com");
        var secondOwnerId = await database.CreateUserAsync("second@example.com");
        var handler = CreateHandler(
            database,
            ControlledExternalCourseProvider.ForSoftwareEngineering());

        var first = await handler.RegisterAsync(
            firstOwnerId,
            "https://mock-moodle.local/courses/software-engineering-2026");
        var second = await handler.RegisterAsync(
            secondOwnerId,
            "https://mock-moodle.local/course/view.php?id=se-2026");

        Assert.Equal(CourseRegistrationOutcome.Created, first.Outcome);
        Assert.Equal(CourseRegistrationOutcome.Created, second.Outcome);
        Assert.NotEqual(first.Subscription!.Id, second.Subscription!.Id);
        Assert.NotEqual(first.Subscription.ModuleId, second.Subscription.ModuleId);
        Assert.Single(database.Context.ExternalCourses);
        Assert.Equal(2, database.Context.CourseSubscriptions.Count());
        Assert.Equal(2, database.Context.Modules.Count());
        Assert.Contains(database.Context.Modules, module => module.OwnerId == firstOwnerId);
        Assert.Contains(database.Context.Modules, module => module.OwnerId == secondOwnerId);
    }

    [Fact]
    public async Task RegisterAsync_SubscriptionPersistenceFails_RollsBackCourseAndModule()
    {
        await using var database = await ExternalCourseTestDatabase.CreateAsync();
        var ownerId = await database.CreateUserAsync("student@example.com");
        await database.Context.Database.ExecuteSqlRawAsync(
            """
            CREATE TRIGGER fail_course_subscription_insert
            BEFORE INSERT ON course_subscriptions
            BEGIN
                SELECT RAISE(ABORT, 'forced subscription persistence failure');
            END;
            """);
        var handler = CreateHandler(
            database,
            ControlledExternalCourseProvider.ForSoftwareEngineering());

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            handler.RegisterAsync(
                ownerId,
                "https://mock-moodle.local/courses/software-engineering-2026"));
        database.Context.ChangeTracker.Clear();

        Assert.Empty(await database.Context.ExternalCourses.ToListAsync());
        Assert.Empty(await database.Context.CourseSubscriptions.ToListAsync());
        Assert.Empty(await database.Context.Modules.ToListAsync());
    }

    [Fact]
    public async Task RegisterAsync_SupportedCourse_DoesNotFetchSnapshot()
    {
        await using var database = await ExternalCourseTestDatabase.CreateAsync();
        var ownerId = await database.CreateUserAsync("student@example.com");
        var provider = ControlledExternalCourseProvider.ForSoftwareEngineering();
        var handler = CreateHandler(database, provider);

        var result = await handler.RegisterAsync(
            ownerId,
            "https://mock-moodle.local/courses/software-engineering-2026");

        Assert.Equal(CourseRegistrationOutcome.Created, result.Outcome);
        Assert.Equal(0, provider.FetchCount);
        Assert.Empty(database.Context.ExternalContents);
    }

    [Fact]
    public async Task RegisterAsync_UniqueSubscriptionRaceForSameOwner_ReconcilesExactExistingSubscription()
    {
        await using var database = await ExternalCourseTestDatabase.CreateAsync();
        var ownerId = await database.CreateUserAsync("student@example.com");
        var otherOwnerId = await database.CreateUserAsync("other@example.com");
        var course = new ExternalCourse(
            "mock-moodle",
            "software-engineering-2026",
            "Software Engineering",
            database.Now);
        var existingModule = new StudyModule(ownerId, "Concurrent module");
        var existingSubscription = new CourseSubscription(
            ownerId,
            course.Id,
            existingModule.Id,
            database.Now);
        var otherModule = new StudyModule(otherOwnerId, "Other module");
        var otherSubscription = new CourseSubscription(
            otherOwnerId,
            course.Id,
            otherModule.Id,
            database.Now);
        database.Context.AddRange(
            course,
            existingModule,
            existingSubscription,
            otherModule,
            otherSubscription);
        await database.Context.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();
        var race = await ConfigureSubscriptionRaceAsync(
            database,
            revealSubscriptionsAfterFirstConflict: true);
        var handler = CreateHandler(database, ControlledExternalCourseProvider.ForSoftwareEngineering());

        var result = await handler.RegisterAsync(
            ownerId,
            "https://mock-moodle.local/courses/software-engineering-2026");

        Assert.Equal(CourseRegistrationOutcome.Existing, result.Outcome);
        Assert.Equal(existingSubscription.Id, result.Subscription!.Id);
        Assert.Equal(existingSubscription.ModuleId, result.Subscription.ModuleId);
        Assert.Equal(1, race.InsertAttempts);
        Assert.Single(await database.Context.ExternalCourses.ToListAsync());
        Assert.Equal(2, await database.Context.CourseSubscriptions.CountAsync());
        Assert.Equal(2, await database.Context.Modules.CountAsync());
    }

    [Fact]
    public async Task RegisterAsync_UniqueCanonicalCourseRaceForAnotherOwner_RetriesOwnerSubscription()
    {
        await using var database = await ExternalCourseTestDatabase.CreateAsync();
        var ownerId = await database.CreateUserAsync("student@example.com");
        var otherOwnerId = await database.CreateUserAsync("other@example.com");
        var sharedCourse = new ExternalCourse(
            "mock-moodle",
            "software-engineering-2026",
            "Software Engineering",
            database.Now);
        var otherModule = new StudyModule(otherOwnerId, "Other module");
        database.Context.AddRange(
            sharedCourse,
            otherModule,
            new CourseSubscription(
                otherOwnerId,
                sharedCourse.Id,
                otherModule.Id,
                database.Now));
        await database.Context.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();
        var race = await ConfigureSubscriptionRaceAsync(
            database,
            revealSubscriptionsAfterFirstConflict: true);
        var handler = CreateHandler(database, ControlledExternalCourseProvider.ForSoftwareEngineering());

        var result = await handler.RegisterAsync(
            ownerId,
            "https://mock-moodle.local/courses/software-engineering-2026");

        Assert.Equal(CourseRegistrationOutcome.Created, result.Outcome);
        Assert.Equal(ownerId, (await database.Context.CourseSubscriptions
            .SingleAsync(item => item.Id == result.Subscription!.Id)).OwnerId);
        Assert.Single(await database.Context.ExternalCourses.ToListAsync());
        Assert.Equal(2, await database.Context.CourseSubscriptions.CountAsync());
        Assert.Equal(2, await database.Context.Modules.CountAsync());
        Assert.Equal(2, race.InsertAttempts);
    }

    [Fact]
    public async Task RegisterAsync_SecondUniqueConflictAfterCanonicalRetry_Propagates()
    {
        await using var database = await ExternalCourseTestDatabase.CreateAsync();
        var ownerId = await database.CreateUserAsync("student@example.com");
        var course = new ExternalCourse(
            "mock-moodle",
            "software-engineering-2026",
            "Software Engineering",
            database.Now);
        var existingModule = new StudyModule(ownerId, "Existing module");
        database.Context.AddRange(
            course,
            existingModule,
            new CourseSubscription(
                ownerId,
                course.Id,
                existingModule.Id,
                database.Now));
        await database.Context.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();
        var race = await ConfigureSubscriptionRaceAsync(
            database,
            revealSubscriptionsAfterFirstConflict: false);
        var handler = CreateHandler(database, ControlledExternalCourseProvider.ForSoftwareEngineering());

        await Assert.ThrowsAsync<DbUpdateException>(() => handler.RegisterAsync(
            ownerId,
            "https://mock-moodle.local/courses/software-engineering-2026"));

        Assert.Equal(2, race.InsertAttempts);
        race.RevealSubscriptions();
        Assert.Single(await database.Context.ExternalCourses.ToListAsync());
        Assert.Single(await database.Context.CourseSubscriptions.ToListAsync());
        Assert.Single(await database.Context.Modules.ToListAsync());
    }

    private static ExternalCourseRegistrationHandler CreateHandler(
        ExternalCourseTestDatabase database,
        IExternalCourseProvider provider)
    {
        return new ExternalCourseRegistrationHandler(
            database.Context,
            [provider],
            database.TimeProvider);
    }

    private static async Task<SubscriptionRace> ConfigureSubscriptionRaceAsync(
        ExternalCourseTestDatabase database,
        bool revealSubscriptionsAfterFirstConflict)
    {
        var race = new SubscriptionRace(revealSubscriptionsAfterFirstConflict);
        database.Connection.CreateFunction("show_subscription_rows", () =>
            race.SubscriptionsVisible ? 1 : 0);
        database.Connection.CreateFunction("prepare_subscription_insert", () =>
        {
            race.InsertAttempts++;
            if (race.RevealSubscriptionsAfterFirstConflict)
            {
                race.RevealSubscriptions();
            }

            return 0;
        });
        database.Connection.CreateFunction("force_first_subscription_conflict", () =>
            race.InsertAttempts == 1 ? 1 : 0);

        await database.Context.Database.ExecuteSqlRawAsync(
            "ALTER TABLE course_subscriptions RENAME TO course_subscriptions_store");
        await database.Context.Database.ExecuteSqlRawAsync(
            """
            CREATE VIEW course_subscriptions AS
            SELECT id, owner_id, external_course_id, module_id, created_at_utc
            FROM course_subscriptions_store
            WHERE show_subscription_rows() = 1
            """);
        await database.Context.Database.ExecuteSqlRawAsync(
            """
            CREATE TRIGGER insert_course_subscription
            INSTEAD OF INSERT ON course_subscriptions
            BEGIN
                SELECT prepare_subscription_insert();
                SELECT CASE WHEN force_first_subscription_conflict() = 1
                    THEN RAISE(ABORT, 'UNIQUE constraint failed: course_subscriptions.owner_id, course_subscriptions.external_course_id')
                END;
                INSERT INTO course_subscriptions_store (
                    id, owner_id, external_course_id, module_id, created_at_utc)
                VALUES (
                    NEW.id, NEW.owner_id, NEW.external_course_id, NEW.module_id, NEW.created_at_utc);
            END
            """);

        return race;
    }

    private sealed class SubscriptionRace(bool revealSubscriptionsAfterFirstConflict)
    {
        public bool RevealSubscriptionsAfterFirstConflict { get; } =
            revealSubscriptionsAfterFirstConflict;

        public bool SubscriptionsVisible { get; private set; }

        public int InsertAttempts { get; set; }

        public void RevealSubscriptions()
        {
            SubscriptionsVisible = true;
        }
    }
}
