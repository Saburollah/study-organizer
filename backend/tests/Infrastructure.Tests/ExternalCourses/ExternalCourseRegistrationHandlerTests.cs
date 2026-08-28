using Microsoft.EntityFrameworkCore;
using StudyOrganizer.Application.ExternalCourses;
using StudyOrganizer.Infrastructure.ExternalCourses;

namespace StudyOrganizer.Infrastructure.Tests.ExternalCourses;

public sealed class ExternalCourseRegistrationHandlerTests
{
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

    private static ExternalCourseRegistrationHandler CreateHandler(
        ExternalCourseTestDatabase database,
        IExternalCourseProvider provider)
    {
        return new ExternalCourseRegistrationHandler(
            database.Context,
            [provider],
            database.TimeProvider);
    }
}
