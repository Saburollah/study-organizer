using Microsoft.EntityFrameworkCore;
using Npgsql;
using StudyOrganizer.Domain.ExternalCourses;
using StudyOrganizer.Domain.Modules;
using StudyOrganizer.Domain.Tasks;
using StudyOrganizer.Infrastructure.Identity;

namespace StudyOrganizer.Infrastructure.Tests;

public sealed class CourseImportPersistenceTests
    : IClassFixture<PostgreSqlFixture>, IAsyncLifetime
{
    private readonly PostgreSqlFixture _fixture;

    public CourseImportPersistenceTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync()
    {
        return _fixture.ResetDatabaseAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task SaveChangesAsync_PersistsCompleteCourseImportGraph()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var createdAt = DateTimeOffset.UnixEpoch;

        var user = CreateUser();
        var module = new StudyModule(user.Id, "Software Engineering");
        var course = new ExternalCourse(
            new ExternalCourseIdentity(
                "mock-moodle",
                "https://moodle.example.test",
                "course-17"),
            "Software Engineering",
            createdAt);
        course.Activate();

        var subscription = new CourseSubscription(
            module.Id,
            user.Id,
            course.Id,
            createdAt);
        subscription.Activate(createdAt.AddMinutes(1));

        var content = new ExternalLearningContent(
            course.Id,
            new ExternalContentKey("file-17"),
            ExternalLearningContentType.File,
            "Exercise sheet",
            createdAt,
            null,
            "application/pdf",
            "/mod/resource/17");

        var scanRun = new ScanRun(
            course.Id,
            createdAt,
            createdAt.AddMinutes(5));
        scanRun.Succeed(
            new ScanRunCounts(1, 0, 0, 0),
            createdAt.AddMinutes(1));

        var snapshot = new CourseSnapshot(
            course.Id,
            scanRun.Id,
            createdAt.AddMinutes(1));
        var snapshotItem = new CourseSnapshotItem(
            snapshot.Id,
            course.Id,
            content.Id,
            content.ExternalContentKey,
            content.Type,
            content.Title,
            content.DueDate,
            content.MediaType,
            content.SourceReference);

        var task = new StudyTask(
            module.Id,
            "Exercise sheet",
            null);
        var importState = new SubscriptionContentState(
            subscription.Id,
            course.Id,
            content.Id,
            task.Id,
            content.Signature,
            createdAt.AddMinutes(1));
        var sourceUpdate = new SourceUpdate(
            importState.Id,
            ContentSignature.Compute(
                ExternalLearningContentType.File,
                "Renamed exercise sheet",
                null,
                "application/pdf",
                "/mod/resource/17",
                ExternalLearningContentAvailability.Available),
            createdAt.AddMinutes(2),
            scanRun.Id);

        context.Users.Add(user);
        context.Modules.Add(module);
        context.ExternalCourses.Add(course);
        context.CourseSubscriptions.Add(subscription);
        context.ExternalLearningContents.Add(content);
        context.ScanRuns.Add(scanRun);
        context.CourseSnapshots.Add(snapshot);
        context.CourseSnapshotItems.Add(snapshotItem);
        context.Tasks.Add(task);
        context.SubscriptionContentStates.Add(importState);
        context.SourceUpdates.Add(sourceUpdate);

        // Act
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        // Assert
        var savedCourse = await context.ExternalCourses.SingleAsync();
        var savedSubscription =
            await context.CourseSubscriptions.SingleAsync();
        var savedContent =
            await context.ExternalLearningContents.SingleAsync();
        var savedScan = await context.ScanRuns.SingleAsync();
        var savedSnapshot = await context.CourseSnapshots.SingleAsync();
        var savedItem = await context.CourseSnapshotItems.SingleAsync();
        var savedState =
            await context.SubscriptionContentStates.SingleAsync();
        var savedUpdate = await context.SourceUpdates.SingleAsync();

        Assert.Equal(course.Identity, savedCourse.Identity);
        Assert.Equal(subscription.State, savedSubscription.State);
        Assert.Equal(content.Signature, savedContent.Signature);
        Assert.Equal(scanRun.Counts, savedScan.Counts);
        Assert.True(savedSnapshot.IsCurrent);
        Assert.Equal(snapshotItem.Signature, savedItem.Signature);
        Assert.Equal(importState.Status, savedState.Status);
        Assert.Equal(
            sourceUpdate.DetectedSignature,
            savedUpdate.DetectedSignature);
    }

    [Fact]
    public async Task SaveChangesAsync_WithDuplicateCourseIdentity_IsRejected()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        context.ExternalCourses.AddRange(
            CreateCourse("course-17"),
            CreateCourse("course-17"));

        // Act
        var action = () => context.SaveChangesAsync();

        // Assert
        await Assert.ThrowsAsync<DbUpdateException>(action);
    }

    [Fact]
    public async Task SaveChangesAsync_WithDuplicateContentKeyInCourse_IsRejected()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var course = CreateCourse("course-17");
        context.ExternalCourses.Add(course);
        context.ExternalLearningContents.AddRange(
            CreateContent(course.Id, "file-17"),
            CreateContent(course.Id, "file-17"));

        // Act
        var action = () => context.SaveChangesAsync();

        // Assert
        await Assert.ThrowsAsync<DbUpdateException>(action);
    }

    [Fact]
    public async Task SaveChangesAsync_WithTwoSubscriptionsForModule_IsRejected()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var user = CreateUser();
        var module = new StudyModule(user.Id, "Software Engineering");
        var firstCourse = CreateCourse("course-17");
        var secondCourse = CreateCourse("course-18");

        context.Users.Add(user);
        context.Modules.Add(module);
        context.ExternalCourses.AddRange(firstCourse, secondCourse);
        context.CourseSubscriptions.AddRange(
            new CourseSubscription(
                module.Id,
                user.Id,
                firstCourse.Id,
                DateTimeOffset.UnixEpoch),
            new CourseSubscription(
                module.Id,
                user.Id,
                secondCourse.Id,
                DateTimeOffset.UnixEpoch));

        // Act
        var action = () => context.SaveChangesAsync();

        // Assert
        await Assert.ThrowsAsync<DbUpdateException>(action);
    }

    [Fact]
    public async Task SaveChangesAsync_WithTwoSubscriptionsForOwnerAndCourse_IsRejected()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var user = CreateUser();
        var firstModule = new StudyModule(user.Id, "Module A");
        var secondModule = new StudyModule(user.Id, "Module B");
        var course = CreateCourse("course-17");

        context.Users.Add(user);
        context.Modules.AddRange(firstModule, secondModule);
        context.ExternalCourses.Add(course);
        context.CourseSubscriptions.AddRange(
            new CourseSubscription(
                firstModule.Id,
                user.Id,
                course.Id,
                DateTimeOffset.UnixEpoch),
            new CourseSubscription(
                secondModule.Id,
                user.Id,
                course.Id,
                DateTimeOffset.UnixEpoch));

        // Act
        var action = () => context.SaveChangesAsync();

        // Assert
        await Assert.ThrowsAsync<DbUpdateException>(action);
    }

    [Fact]
    public async Task SaveChangesAsync_WithTwoRunningScansForCourse_IsRejected()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var course = CreateCourse("course-17");
        context.ExternalCourses.Add(course);
        context.ScanRuns.AddRange(
            CreateRunningScan(course.Id),
            CreateRunningScan(course.Id));

        // Act
        var action = () => context.SaveChangesAsync();

        // Assert
        await Assert.ThrowsAsync<DbUpdateException>(action);
    }

    [Fact]
    public async Task SaveChangesAsync_WithTwoTerminalScansForCourse_Succeeds()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var course = CreateCourse("course-17");
        var firstScan = CreateRunningScan(course.Id);
        var secondScan = CreateRunningScan(course.Id);
        firstScan.Fail(
            ScanRunErrorCode.SourceUnreachable,
            DateTimeOffset.UnixEpoch.AddMinutes(1));
        secondScan.Fail(
            ScanRunErrorCode.Timeout,
            DateTimeOffset.UnixEpoch.AddMinutes(2));

        context.ExternalCourses.Add(course);
        context.ScanRuns.AddRange(firstScan, secondScan);

        // Act
        await context.SaveChangesAsync();

        // Assert
        Assert.Equal(2, await context.ScanRuns.CountAsync());
    }

    [Fact]
    public async Task SaveChangesAsync_WithTwoCurrentSnapshotsForCourse_IsRejected()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var course = CreateCourse("course-17");
        var firstScan = CreateSucceededScan(course.Id, 1);
        var secondScan = CreateSucceededScan(course.Id, 2);

        context.ExternalCourses.Add(course);
        context.ScanRuns.AddRange(firstScan, secondScan);
        context.CourseSnapshots.AddRange(
            new CourseSnapshot(
                course.Id,
                firstScan.Id,
                DateTimeOffset.UnixEpoch.AddMinutes(1)),
            new CourseSnapshot(
                course.Id,
                secondScan.Id,
                DateTimeOffset.UnixEpoch.AddMinutes(2)));

        // Act
        var action = () => context.SaveChangesAsync();

        // Assert
        await Assert.ThrowsAsync<DbUpdateException>(action);
    }

    [Fact]
    public async Task SaveChangesAsync_WithHistoricalAndCurrentSnapshot_Succeeds()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var course = CreateCourse("course-17");
        var firstScan = CreateSucceededScan(course.Id, 1);
        var secondScan = CreateSucceededScan(course.Id, 2);
        var historicalSnapshot = new CourseSnapshot(
            course.Id,
            firstScan.Id,
            DateTimeOffset.UnixEpoch.AddMinutes(1));
        historicalSnapshot.MarkSuperseded();
        var currentSnapshot = new CourseSnapshot(
            course.Id,
            secondScan.Id,
            DateTimeOffset.UnixEpoch.AddMinutes(2));

        context.ExternalCourses.Add(course);
        context.ScanRuns.AddRange(firstScan, secondScan);
        context.CourseSnapshots.AddRange(
            historicalSnapshot,
            currentSnapshot);

        // Act
        await context.SaveChangesAsync();

        // Assert
        Assert.Equal(2, await context.CourseSnapshots.CountAsync());
    }

    [Fact]
    public async Task SaveChangesAsync_WithImportedAndDismissedState_IsRejected()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var graph = CreateSubscriptionGraph();
        var firstTask = new StudyTask(
            graph.Module.Id,
            "First import",
            null);
        var secondTask = new StudyTask(
            graph.Module.Id,
            "Second import",
            null);
        var dismissedState = new SubscriptionContentState(
            graph.Subscription.Id,
            graph.Course.Id,
            graph.Content.Id,
            firstTask.Id,
            graph.Content.Signature,
            DateTimeOffset.UnixEpoch);
        dismissedState.Dismiss(DateTimeOffset.UnixEpoch.AddMinutes(1));
        var importedState = new SubscriptionContentState(
            graph.Subscription.Id,
            graph.Course.Id,
            graph.Content.Id,
            secondTask.Id,
            graph.Content.Signature,
            DateTimeOffset.UnixEpoch.AddMinutes(1));

        AddSubscriptionGraph(context, graph);
        context.Tasks.AddRange(firstTask, secondTask);
        context.SubscriptionContentStates.AddRange(
            dismissedState,
            importedState);

        // Act
        var action = () => context.SaveChangesAsync();

        // Assert
        await Assert.ThrowsAsync<DbUpdateException>(action);
    }

    [Fact]
    public async Task Database_WithImportedStateWithoutTask_IsRejected()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var graph = CreateSubscriptionGraph();
        var task = new StudyTask(graph.Module.Id, "Import", null);
        var state = new SubscriptionContentState(
            graph.Subscription.Id,
            graph.Course.Id,
            graph.Content.Id,
            task.Id,
            graph.Content.Signature,
            DateTimeOffset.UnixEpoch);
        AddSubscriptionGraph(context, graph);
        context.Tasks.Add(task);
        context.SubscriptionContentStates.Add(state);
        await context.SaveChangesAsync();

        // Act
        var action = () => context.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE subscription_content_states SET study_task_id = NULL WHERE id = {state.Id}");

        // Assert
        var exception = await Assert.ThrowsAsync<PostgresException>(action);
        Assert.Equal(PostgresErrorCodes.CheckViolation, exception.SqlState);
    }

    [Fact]
    public async Task SaveChangesAsync_WithTaskImportedTwice_IsRejected()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var graph = CreateSubscriptionGraph();
        var secondContent = CreateContent(graph.Course.Id, "file-18");
        var task = new StudyTask(graph.Module.Id, "Import", null);

        AddSubscriptionGraph(context, graph);
        context.ExternalLearningContents.Add(secondContent);
        context.Tasks.Add(task);
        context.SubscriptionContentStates.AddRange(
            new SubscriptionContentState(
                graph.Subscription.Id,
                graph.Course.Id,
                graph.Content.Id,
                task.Id,
                graph.Content.Signature,
                DateTimeOffset.UnixEpoch),
            new SubscriptionContentState(
                graph.Subscription.Id,
                graph.Course.Id,
                secondContent.Id,
                task.Id,
                secondContent.Signature,
                DateTimeOffset.UnixEpoch));

        // Act
        var action = () => context.SaveChangesAsync();

        // Assert
        await Assert.ThrowsAsync<DbUpdateException>(action);
    }

    [Fact]
    public async Task SaveChangesAsync_WithContentFromDifferentCourse_IsRejected()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var graph = CreateSubscriptionGraph();
        var otherCourse = CreateCourse("course-18");
        var otherContent = CreateContent(otherCourse.Id, "file-18");
        var task = new StudyTask(graph.Module.Id, "Import", null);
        var mismatchedState = new SubscriptionContentState(
            graph.Subscription.Id,
            graph.Course.Id,
            otherContent.Id,
            task.Id,
            otherContent.Signature,
            DateTimeOffset.UnixEpoch);

        AddSubscriptionGraph(context, graph);
        context.ExternalCourses.Add(otherCourse);
        context.ExternalLearningContents.Add(otherContent);
        context.Tasks.Add(task);
        context.SubscriptionContentStates.Add(mismatchedState);

        // Act
        var action = () => context.SaveChangesAsync();

        // Assert
        await Assert.ThrowsAsync<DbUpdateException>(action);
    }

    [Fact]
    public async Task SaveChangesAsync_WithUnknownModuleOwner_IsRejected()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        context.Modules.Add(
            new StudyModule(Guid.NewGuid(), "Orphaned module"));

        // Act
        var action = () => context.SaveChangesAsync();

        // Assert
        await Assert.ThrowsAsync<DbUpdateException>(action);
    }

    [Fact]
    public async Task SaveChangesAsync_WithSnapshotItemFromDifferentCourse_IsRejected()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var snapshotCourse = CreateCourse("course-17");
        var contentCourse = CreateCourse("course-18");
        var content = CreateContent(contentCourse.Id, "file-18");
        var scan = CreateSucceededScan(snapshotCourse.Id, 1);
        var snapshot = new CourseSnapshot(
            snapshotCourse.Id,
            scan.Id,
            DateTimeOffset.UnixEpoch.AddMinutes(1));
        var mismatchedItem = new CourseSnapshotItem(
            snapshot.Id,
            snapshotCourse.Id,
            content.Id,
            content.ExternalContentKey,
            content.Type,
            content.Title,
            content.DueDate,
            content.MediaType,
            content.SourceReference);

        context.ExternalCourses.AddRange(snapshotCourse, contentCourse);
        context.ExternalLearningContents.Add(content);
        context.ScanRuns.Add(scan);
        context.CourseSnapshots.Add(snapshot);
        context.CourseSnapshotItems.Add(mismatchedItem);

        // Act
        var action = () => context.SaveChangesAsync();

        // Assert
        await Assert.ThrowsAsync<DbUpdateException>(action);
    }

    [Fact]
    public async Task SaveChangesAsync_WithDuplicateSnapshotItemKey_IsRejected()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var course = CreateCourse("course-17");
        var firstContent = CreateContent(course.Id, "file-17");
        var secondContent = CreateContent(course.Id, "file-18");
        var scan = CreateSucceededScan(course.Id, 1);
        var snapshot = new CourseSnapshot(
            course.Id,
            scan.Id,
            DateTimeOffset.UnixEpoch.AddMinutes(1));
        var duplicateKey = new ExternalContentKey("duplicate-key");

        context.ExternalCourses.Add(course);
        context.ExternalLearningContents.AddRange(
            firstContent,
            secondContent);
        context.ScanRuns.Add(scan);
        context.CourseSnapshots.Add(snapshot);
        context.CourseSnapshotItems.AddRange(
            new CourseSnapshotItem(
                snapshot.Id,
                course.Id,
                firstContent.Id,
                duplicateKey,
                firstContent.Type,
                firstContent.Title,
                firstContent.DueDate,
                firstContent.MediaType,
                firstContent.SourceReference),
            new CourseSnapshotItem(
                snapshot.Id,
                course.Id,
                secondContent.Id,
                duplicateKey,
                secondContent.Type,
                secondContent.Title,
                secondContent.DueDate,
                secondContent.MediaType,
                secondContent.SourceReference));

        // Act
        var action = () => context.SaveChangesAsync();

        // Assert
        await Assert.ThrowsAsync<DbUpdateException>(action);
    }

    [Fact]
    public async Task DeleteExternalCourse_WithDependents_IsRestricted()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var course = CreateCourse("course-17");
        context.ExternalCourses.Add(course);
        context.ExternalLearningContents.Add(
            CreateContent(course.Id, "file-17"));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var savedCourse = await context.ExternalCourses
            .SingleAsync(candidate => candidate.Id == course.Id);
        context.ExternalCourses.Remove(savedCourse);

        // Act
        var action = () => context.SaveChangesAsync();

        // Assert
        await Assert.ThrowsAsync<DbUpdateException>(action);
    }

    [Fact]
    public async Task DeleteCourseSnapshot_CascadesToSnapshotItems()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var course = CreateCourse("course-17");
        var content = CreateContent(course.Id, "file-17");
        var scan = CreateSucceededScan(course.Id, 1);
        var snapshot = new CourseSnapshot(
            course.Id,
            scan.Id,
            DateTimeOffset.UnixEpoch.AddMinutes(1));
        var item = new CourseSnapshotItem(
            snapshot.Id,
            course.Id,
            content.Id,
            content.ExternalContentKey,
            content.Type,
            content.Title,
            content.DueDate,
            content.MediaType,
            content.SourceReference);

        context.ExternalCourses.Add(course);
        context.ExternalLearningContents.Add(content);
        context.ScanRuns.Add(scan);
        context.CourseSnapshots.Add(snapshot);
        context.CourseSnapshotItems.Add(item);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var savedSnapshot = await context.CourseSnapshots
            .SingleAsync(candidate => candidate.Id == snapshot.Id);

        // Act
        context.CourseSnapshots.Remove(savedSnapshot);
        await context.SaveChangesAsync();

        // Assert
        Assert.Empty(await context.CourseSnapshotItems.ToListAsync());
    }

    [Fact]
    public async Task DeleteSubscriptionContentState_CascadesToSourceUpdate()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var graph = CreateSubscriptionGraph();
        var task = new StudyTask(graph.Module.Id, "Import", null);
        var state = new SubscriptionContentState(
            graph.Subscription.Id,
            graph.Course.Id,
            graph.Content.Id,
            task.Id,
            graph.Content.Signature,
            DateTimeOffset.UnixEpoch);
        var update = new SourceUpdate(
            state.Id,
            CreateSignature("Renamed exercise sheet"),
            DateTimeOffset.UnixEpoch.AddMinutes(1));

        AddSubscriptionGraph(context, graph);
        context.Tasks.Add(task);
        context.SubscriptionContentStates.Add(state);
        context.SourceUpdates.Add(update);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var savedState = await context.SubscriptionContentStates
            .SingleAsync(candidate => candidate.Id == state.Id);

        // Act
        context.SubscriptionContentStates.Remove(savedState);
        await context.SaveChangesAsync();

        // Assert
        Assert.Empty(await context.SourceUpdates.ToListAsync());
    }

    [Fact]
    public async Task SaveChangesAsync_WithTwoOpenSourceUpdates_IsRejected()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var graph = CreateSubscriptionGraph();
        var task = new StudyTask(graph.Module.Id, "Import", null);
        var state = new SubscriptionContentState(
            graph.Subscription.Id,
            graph.Course.Id,
            graph.Content.Id,
            task.Id,
            graph.Content.Signature,
            DateTimeOffset.UnixEpoch);

        AddSubscriptionGraph(context, graph);
        context.Tasks.Add(task);
        context.SubscriptionContentStates.Add(state);
        context.SourceUpdates.AddRange(
            new SourceUpdate(
                state.Id,
                CreateSignature("Renamed once"),
                DateTimeOffset.UnixEpoch.AddMinutes(1)),
            new SourceUpdate(
                state.Id,
                CreateSignature("Renamed twice"),
                DateTimeOffset.UnixEpoch.AddMinutes(2)));

        // Act
        var action = () => context.SaveChangesAsync();

        // Assert
        await Assert.ThrowsAsync<DbUpdateException>(action);
    }

    [Fact]
    public async Task DeleteScanRun_SetsSourceUpdateReferenceToNull()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var graph = CreateSubscriptionGraph();
        var task = new StudyTask(graph.Module.Id, "Import", null);
        var state = new SubscriptionContentState(
            graph.Subscription.Id,
            graph.Course.Id,
            graph.Content.Id,
            task.Id,
            graph.Content.Signature,
            DateTimeOffset.UnixEpoch);
        var scan = CreateSucceededScan(graph.Course.Id, 1);
        var update = new SourceUpdate(
            state.Id,
            CreateSignature("Renamed exercise sheet"),
            DateTimeOffset.UnixEpoch.AddMinutes(1),
            scan.Id);

        AddSubscriptionGraph(context, graph);
        context.Tasks.Add(task);
        context.SubscriptionContentStates.Add(state);
        context.ScanRuns.Add(scan);
        context.SourceUpdates.Add(update);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var savedScan = await context.ScanRuns
            .SingleAsync(candidate => candidate.Id == scan.Id);

        // Act
        context.ScanRuns.Remove(savedScan);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        // Assert
        var savedUpdate = await context.SourceUpdates.SingleAsync();
        Assert.Null(savedUpdate.DetectedByScanRunId);
    }

    private static ExternalCourse CreateCourse(string externalCourseKey)
    {
        return new ExternalCourse(
            new ExternalCourseIdentity(
                "mock-moodle",
                "https://moodle.example.test",
                externalCourseKey),
            $"Course {externalCourseKey}",
            DateTimeOffset.UnixEpoch);
    }

    private static ExternalLearningContent CreateContent(
        Guid externalCourseId,
        string externalContentKey)
    {
        return new ExternalLearningContent(
            externalCourseId,
            new ExternalContentKey(externalContentKey),
            ExternalLearningContentType.File,
            "Exercise sheet",
            DateTimeOffset.UnixEpoch,
            null,
            "application/pdf",
            $"/mod/resource/{externalContentKey}");
    }

    private static ScanRun CreateRunningScan(Guid externalCourseId)
    {
        return new ScanRun(
            externalCourseId,
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch.AddMinutes(5));
    }

    private static ScanRun CreateSucceededScan(
        Guid externalCourseId,
        int completedMinute)
    {
        var scan = CreateRunningScan(externalCourseId);
        scan.Succeed(
            new ScanRunCounts(0, 0, 0, 0),
            DateTimeOffset.UnixEpoch.AddMinutes(completedMinute));
        return scan;
    }

    private static ContentSignature CreateSignature(string title)
    {
        return ContentSignature.Compute(
            ExternalLearningContentType.File,
            title,
            null,
            "application/pdf",
            "/mod/resource/17",
            ExternalLearningContentAvailability.Available);
    }

    private static SubscriptionGraph CreateSubscriptionGraph()
    {
        var user = CreateUser();
        var module = new StudyModule(user.Id, "Software Engineering");
        var course = CreateCourse("course-17");
        var subscription = new CourseSubscription(
            module.Id,
            user.Id,
            course.Id,
            DateTimeOffset.UnixEpoch);
        var content = CreateContent(course.Id, "file-17");
        return new SubscriptionGraph(
            user,
            module,
            course,
            subscription,
            content);
    }

    private static void AddSubscriptionGraph(
        StudyOrganizer.Infrastructure.Persistence.ApplicationDbContext context,
        SubscriptionGraph graph)
    {
        context.Users.Add(graph.User);
        context.Modules.Add(graph.Module);
        context.ExternalCourses.Add(graph.Course);
        context.CourseSubscriptions.Add(graph.Subscription);
        context.ExternalLearningContents.Add(graph.Content);
    }

    private static ApplicationUser CreateUser()
    {
        return new ApplicationUser
        {
            Email = "student@example.test",
            NormalizedEmail = "STUDENT@EXAMPLE.TEST",
            UserName = "student@example.test",
            NormalizedUserName = "STUDENT@EXAMPLE.TEST"
        };
    }

    private sealed record SubscriptionGraph(
        ApplicationUser User,
        StudyModule Module,
        ExternalCourse Course,
        CourseSubscription Subscription,
        ExternalLearningContent Content);
}
