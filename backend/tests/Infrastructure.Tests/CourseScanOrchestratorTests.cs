using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using StudyOrganizer.Application.ExternalCourses;
using StudyOrganizer.Domain.ExternalCourses;
using StudyOrganizer.Domain.Modules;
using StudyOrganizer.Domain.Tasks;
using StudyOrganizer.Infrastructure.ExternalCourses;
using StudyOrganizer.Infrastructure.Identity;
using StudyOrganizer.Infrastructure.Persistence;

namespace StudyOrganizer.Infrastructure.Tests;

public sealed class CourseScanOrchestratorTests
    : IClassFixture<PostgreSqlFixture>, IAsyncLifetime
{
    private readonly PostgreSqlFixture _fixture;

    public CourseScanOrchestratorTests(PostgreSqlFixture fixture)
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
    public async Task ScanAsync_WithNewPdf_PersistsCompleteImport()
    {
        // Arrange
        await using var arrangeContext = _fixture.CreateDbContext();
        var user = CreateUser();
        var module = new StudyModule(user.Id, "Software Engineering");
        var course = new ExternalCourse(
            new ExternalCourseIdentity(
                "mock-moodle",
                "https://moodle.example.test",
                "course-17"),
            "Software Engineering",
            DateTimeOffset.UnixEpoch);
        course.Activate();
        var subscription = new CourseSubscription(
            module.Id,
            user.Id,
            course.Id,
            DateTimeOffset.UnixEpoch);
        subscription.Activate(DateTimeOffset.UnixEpoch);

        arrangeContext.Users.Add(user);
        arrangeContext.Modules.Add(module);
        arrangeContext.ExternalCourses.Add(course);
        arrangeContext.CourseSubscriptions.Add(subscription);
        await arrangeContext.SaveChangesAsync();

        var source = new StubExternalCourseSource(
            new ExternalCourseSourcePayload(
            [
                new CourseSourceItem(
                    new ExternalContentKey("file-17"),
                    ExternalLearningContentType.File,
                    "Exercise sheet",
                    null,
                    "application/pdf",
                    "/mod/resource/17")
            ]));
        var orchestrator = new CourseScanOrchestrator(
            new TestDbContextFactory(_fixture),
            source,
            new FixedTimeProvider(
                DateTimeOffset.UnixEpoch.AddHours(1)),
            new CourseScanOptions(
                TimeSpan.FromMinutes(5),
                TimeSpan.FromSeconds(30)));

        // Act
        var result = await orchestrator.ScanAsync(course.Id);

        // Assert
        Assert.Equal(ScanRunStatus.Succeeded, result.Status);
        Assert.False(result.ReusedExistingRun);
        Assert.Equal(1, result.Counts.New);

        await using var assertContext = _fixture.CreateDbContext();
        Assert.Equal(1, await assertContext.ScanRuns.CountAsync());
        Assert.Equal(1, await assertContext.CourseSnapshots.CountAsync());
        Assert.Equal(
            1,
            await assertContext.CourseSnapshotItems.CountAsync());
        Assert.Equal(
            1,
            await assertContext.ExternalLearningContents.CountAsync());
        Assert.Equal(1, await assertContext.Tasks.CountAsync());
        Assert.Equal(
            1,
            await assertContext.SubscriptionContentStates.CountAsync());
        Assert.Equal(
            DateTimeOffset.UnixEpoch.AddHours(1),
            await assertContext.Tasks
                .Select(task => task.CreatedAt)
                .SingleAsync());
    }

    [Fact]
    public async Task ScanAsync_WithSameSnapshotTwice_DoesNotDuplicateImports()
    {
        // Arrange
        await using var arrangeContext = _fixture.CreateDbContext();
        var user = CreateUser();
        var module = new StudyModule(user.Id, "Software Engineering");
        var course = new ExternalCourse(
            new ExternalCourseIdentity(
                "mock-moodle",
                "https://moodle.example.test",
                "course-17"),
            "Software Engineering",
            DateTimeOffset.UnixEpoch);
        course.Activate();
        var subscription = new CourseSubscription(
            module.Id,
            user.Id,
            course.Id,
            DateTimeOffset.UnixEpoch);
        subscription.Activate(DateTimeOffset.UnixEpoch);

        arrangeContext.Users.Add(user);
        arrangeContext.Modules.Add(module);
        arrangeContext.ExternalCourses.Add(course);
        arrangeContext.CourseSubscriptions.Add(subscription);
        await arrangeContext.SaveChangesAsync();

        var source = new StubExternalCourseSource(
            new ExternalCourseSourcePayload(
            [
                new CourseSourceItem(
                    new ExternalContentKey("file-17"),
                    ExternalLearningContentType.File,
                    "Exercise sheet",
                    null,
                    "application/pdf",
                    "/mod/resource/17")
            ]));
        var orchestrator = new CourseScanOrchestrator(
            new TestDbContextFactory(_fixture),
            source,
            new FixedTimeProvider(
                DateTimeOffset.UnixEpoch.AddHours(1)),
            new CourseScanOptions(
                TimeSpan.FromMinutes(5),
                TimeSpan.FromSeconds(30)));
        await orchestrator.ScanAsync(course.Id);

        // Act
        var result = await orchestrator.ScanAsync(course.Id);

        // Assert
        Assert.Equal(ScanRunStatus.Succeeded, result.Status);
        Assert.Equal(0, result.Counts.New);
        Assert.Equal(1, result.Counts.Unchanged);

        await using var assertContext = _fixture.CreateDbContext();
        Assert.Equal(2, await assertContext.ScanRuns.CountAsync());
        Assert.Equal(2, await assertContext.CourseSnapshots.CountAsync());
        Assert.Equal(
            1,
            await assertContext.CourseSnapshots.CountAsync(
                snapshot => snapshot.IsCurrent));
        Assert.Equal(
            2,
            await assertContext.CourseSnapshotItems.CountAsync());
        Assert.Equal(
            1,
            await assertContext.ExternalLearningContents.CountAsync());
        Assert.Equal(1, await assertContext.Tasks.CountAsync());
        Assert.Equal(
            1,
            await assertContext.SubscriptionContentStates.CountAsync());
        Assert.Empty(await assertContext.SourceUpdates.ToListAsync());
    }

    [Fact]
    public async Task ScanAsync_WithChangedSource_PreservesPersonalTask()
    {
        // Arrange
        await using var arrangeContext = _fixture.CreateDbContext();
        var user = CreateUser();
        var module = new StudyModule(user.Id, "Software Engineering");
        var course = new ExternalCourse(
            new ExternalCourseIdentity(
                "mock-moodle",
                "https://moodle.example.test",
                "course-17"),
            "Software Engineering",
            DateTimeOffset.UnixEpoch);
        course.Activate();
        var subscription = new CourseSubscription(
            module.Id,
            user.Id,
            course.Id,
            DateTimeOffset.UnixEpoch);
        subscription.Activate(DateTimeOffset.UnixEpoch);

        arrangeContext.Users.Add(user);
        arrangeContext.Modules.Add(module);
        arrangeContext.ExternalCourses.Add(course);
        arrangeContext.CourseSubscriptions.Add(subscription);
        await arrangeContext.SaveChangesAsync();

        var source = new StubExternalCourseSource(
            CreatePayload("Exercise sheet", null));
        var orchestrator = new CourseScanOrchestrator(
            new TestDbContextFactory(_fixture),
            source,
            new FixedTimeProvider(
                DateTimeOffset.UnixEpoch.AddHours(1)),
            new CourseScanOptions(
                TimeSpan.FromMinutes(5),
                TimeSpan.FromSeconds(30)));
        await orchestrator.ScanAsync(course.Id);

        await using (var personalContext = _fixture.CreateDbContext())
        {
            var task = await personalContext.Tasks.SingleAsync();
            task.Update(
                "My personal title",
                DateTimeOffset.UnixEpoch.AddDays(7),
                "My notes");
            task.Complete();
            await personalContext.SaveChangesAsync();
        }

        source.UsePayload(
            CreatePayload(
                "Renamed exercise sheet",
                DateTimeOffset.UnixEpoch.AddDays(2)));

        // Act
        var result = await orchestrator.ScanAsync(course.Id);

        // Assert
        Assert.Equal(1, result.Counts.Updated);

        await using var assertContext = _fixture.CreateDbContext();
        var savedContent =
            await assertContext.ExternalLearningContents.SingleAsync();
        var savedTask = await assertContext.Tasks.SingleAsync();
        var savedState =
            await assertContext.SubscriptionContentStates.SingleAsync();
        var savedUpdate = await assertContext.SourceUpdates.SingleAsync();

        Assert.Equal("Renamed exercise sheet", savedContent.Title);
        Assert.Equal("My personal title", savedTask.Title);
        Assert.Equal("My notes", savedTask.Description);
        Assert.Equal(
            DateTimeOffset.UnixEpoch.AddDays(7),
            savedTask.DueDate);
        Assert.Equal(StudyTaskStatus.Completed, savedTask.Status);
        Assert.NotEqual(
            savedContent.Signature,
            savedState.ConfirmedSignature);
        Assert.Equal(
            savedContent.Signature,
            savedUpdate.DetectedSignature);
        Assert.Equal(result.ScanRunId, savedUpdate.DetectedByScanRunId);
    }

    [Fact]
    public async Task ScanAsync_WhenContentReappears_RestoresSameContent()
    {
        // Arrange
        await using var arrangeContext = _fixture.CreateDbContext();
        var user = CreateUser();
        var module = new StudyModule(user.Id, "Software Engineering");
        var course = new ExternalCourse(
            new ExternalCourseIdentity(
                "mock-moodle",
                "https://moodle.example.test",
                "course-17"),
            "Software Engineering",
            DateTimeOffset.UnixEpoch);
        course.Activate();
        var subscription = new CourseSubscription(
            module.Id,
            user.Id,
            course.Id,
            DateTimeOffset.UnixEpoch);
        subscription.Activate(DateTimeOffset.UnixEpoch);

        arrangeContext.Users.Add(user);
        arrangeContext.Modules.Add(module);
        arrangeContext.ExternalCourses.Add(course);
        arrangeContext.CourseSubscriptions.Add(subscription);
        await arrangeContext.SaveChangesAsync();

        var source = new StubExternalCourseSource(
            CreatePayload("Exercise sheet", null));
        var orchestrator = new CourseScanOrchestrator(
            new TestDbContextFactory(_fixture),
            source,
            new FixedTimeProvider(
                DateTimeOffset.UnixEpoch.AddHours(1)),
            new CourseScanOptions(
                TimeSpan.FromMinutes(5),
                TimeSpan.FromSeconds(30)));
        await orchestrator.ScanAsync(course.Id);

        Guid originalContentId;
        Guid originalTaskId;
        await using (var initialContext = _fixture.CreateDbContext())
        {
            originalContentId =
                await initialContext.ExternalLearningContents
                    .Select(content => content.Id)
                    .SingleAsync();
            originalTaskId = await initialContext.Tasks
                .Select(task => task.Id)
                .SingleAsync();
        }

        source.UsePayload(new ExternalCourseSourcePayload([]));
        var unavailableResult =
            await orchestrator.ScanAsync(course.Id);

        await using (var unavailableContext =
            _fixture.CreateDbContext())
        {
            var unavailableContent =
                await unavailableContext.ExternalLearningContents
                    .SingleAsync();
            Assert.Equal(1, unavailableResult.Counts.Unavailable);
            Assert.Equal(
                ExternalLearningContentAvailability.Unavailable,
                unavailableContent.Availability);
            Assert.Equal(1, await unavailableContext.Tasks.CountAsync());
            Assert.Equal(
                1,
                await unavailableContext.SourceUpdates.CountAsync());
        }

        source.UsePayload(CreatePayload("Exercise sheet", null));

        // Act
        var availableResult = await orchestrator.ScanAsync(course.Id);

        // Assert
        Assert.Equal(1, availableResult.Counts.Updated);

        await using var assertContext = _fixture.CreateDbContext();
        var savedContent =
            await assertContext.ExternalLearningContents.SingleAsync();
        var savedTask = await assertContext.Tasks.SingleAsync();

        Assert.Equal(originalContentId, savedContent.Id);
        Assert.Equal(
            ExternalLearningContentAvailability.Available,
            savedContent.Availability);
        Assert.Equal(originalTaskId, savedTask.Id);
        Assert.Empty(await assertContext.SourceUpdates.ToListAsync());
    }

    [Fact]
    public async Task ScanAsync_WithDuplicateContentKey_RejectsSnapshot()
    {
        // Arrange
        await using var arrangeContext = _fixture.CreateDbContext();
        var course = new ExternalCourse(
            new ExternalCourseIdentity(
                "mock-moodle",
                "https://moodle.example.test",
                "course-17"),
            "Software Engineering",
            DateTimeOffset.UnixEpoch);
        course.Activate();
        arrangeContext.ExternalCourses.Add(course);
        await arrangeContext.SaveChangesAsync();

        var duplicateKey = new ExternalContentKey("file-17");
        var source = new StubExternalCourseSource(
            new ExternalCourseSourcePayload(
            [
                new CourseSourceItem(
                    duplicateKey,
                    ExternalLearningContentType.File,
                    "First occurrence",
                    null,
                    "application/pdf",
                    "/mod/resource/17"),
                new CourseSourceItem(
                    duplicateKey,
                    ExternalLearningContentType.File,
                    "Second occurrence",
                    null,
                    "application/pdf",
                    "/mod/resource/18")
            ]));
        var orchestrator = new CourseScanOrchestrator(
            new TestDbContextFactory(_fixture),
            source,
            new FixedTimeProvider(
                DateTimeOffset.UnixEpoch.AddHours(1)),
            new CourseScanOptions(
                TimeSpan.FromMinutes(5),
                TimeSpan.FromSeconds(30)));

        // Act
        var result = await orchestrator.ScanAsync(course.Id);

        // Assert
        Assert.Equal(ScanRunStatus.Failed, result.Status);
        Assert.Equal(
            ScanRunErrorCode.InvalidSourceData,
            result.ErrorCode);

        await using var assertContext = _fixture.CreateDbContext();
        Assert.Empty(await assertContext.CourseSnapshots.ToListAsync());
        Assert.Empty(
            await assertContext.ExternalLearningContents.ToListAsync());
        Assert.Empty(await assertContext.Tasks.ToListAsync());
        var savedScan = await assertContext.ScanRuns.SingleAsync();
        Assert.Equal(ScanRunStatus.Failed, savedScan.Status);
    }

    [Fact]
    public async Task ScanAsync_WhenSourceTimesOut_PreservesLastSnapshot()
    {
        // Arrange
        await using var arrangeContext = _fixture.CreateDbContext();
        var user = CreateUser();
        var module = new StudyModule(user.Id, "Software Engineering");
        var course = new ExternalCourse(
            new ExternalCourseIdentity(
                "mock-moodle",
                "https://moodle.example.test",
                "course-17"),
            "Software Engineering",
            DateTimeOffset.UnixEpoch);
        course.Activate();
        var subscription = new CourseSubscription(
            module.Id,
            user.Id,
            course.Id,
            DateTimeOffset.UnixEpoch);
        subscription.Activate(DateTimeOffset.UnixEpoch);

        arrangeContext.Users.Add(user);
        arrangeContext.Modules.Add(module);
        arrangeContext.ExternalCourses.Add(course);
        arrangeContext.CourseSubscriptions.Add(subscription);
        await arrangeContext.SaveChangesAsync();

        var source = new StubExternalCourseSource(
            CreatePayload("Exercise sheet", null));
        var orchestrator = new CourseScanOrchestrator(
            new TestDbContextFactory(_fixture),
            source,
            new FixedTimeProvider(
                DateTimeOffset.UnixEpoch.AddHours(1)),
            new CourseScanOptions(
                TimeSpan.FromMinutes(5),
                TimeSpan.FromSeconds(30)));
        await orchestrator.ScanAsync(course.Id);

        Guid currentSnapshotId;
        Guid taskId;
        await using (var initialContext = _fixture.CreateDbContext())
        {
            currentSnapshotId = await initialContext.CourseSnapshots
                .Where(snapshot => snapshot.IsCurrent)
                .Select(snapshot => snapshot.Id)
                .SingleAsync();
            taskId = await initialContext.Tasks
                .Select(task => task.Id)
                .SingleAsync();
        }

        source.UseTimeout();

        // Act
        var result = await orchestrator.ScanAsync(course.Id);

        // Assert
        Assert.Equal(ScanRunStatus.Failed, result.Status);
        Assert.Equal(ScanRunErrorCode.Timeout, result.ErrorCode);

        await using var assertContext = _fixture.CreateDbContext();
        var savedCurrentSnapshotId =
            await assertContext.CourseSnapshots
                .Where(snapshot => snapshot.IsCurrent)
                .Select(snapshot => snapshot.Id)
                .SingleAsync();
        var savedTaskId = await assertContext.Tasks
            .Select(task => task.Id)
            .SingleAsync();

        Assert.Equal(currentSnapshotId, savedCurrentSnapshotId);
        Assert.Equal(taskId, savedTaskId);
        Assert.Equal(2, await assertContext.ScanRuns.CountAsync());
        Assert.Equal(
            1,
            await assertContext.ScanRuns.CountAsync(scan =>
                scan.Status == ScanRunStatus.Failed
                && scan.ErrorCode == ScanRunErrorCode.Timeout));
    }

    [Fact]
    public async Task ScanAsync_Concurrently_ReusesSingleRunningScan()
    {
        // Arrange
        await using var arrangeContext = _fixture.CreateDbContext();
        var course = new ExternalCourse(
            new ExternalCourseIdentity(
                "mock-moodle",
                "https://moodle.example.test",
                "course-17"),
            "Software Engineering",
            DateTimeOffset.UnixEpoch);
        course.Activate();
        arrangeContext.ExternalCourses.Add(course);
        await arrangeContext.SaveChangesAsync();

        var source = new BlockingExternalCourseSource(
            new ExternalCourseSourcePayload([]));
        var orchestrator = new CourseScanOrchestrator(
            new TestDbContextFactory(_fixture),
            source,
            new FixedTimeProvider(
                DateTimeOffset.UnixEpoch.AddHours(1)),
            new CourseScanOptions(
                TimeSpan.FromMinutes(5),
                TimeSpan.FromSeconds(30)));
        var startGate = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var scanTasks = Enumerable.Range(0, 8)
            .Select(_ => Task.Run(async () =>
            {
                await startGate.Task;
                return await orchestrator.ScanAsync(course.Id);
            }))
            .ToArray();
        var allReusedCompleted = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var completedCount = 0;
        foreach (var scanTask in scanTasks)
        {
            _ = scanTask.ContinueWith(
                _ =>
                {
                    if (Interlocked.Increment(ref completedCount) == 7)
                    {
                        allReusedCompleted.TrySetResult();
                    }
                },
                TaskScheduler.Default);
        }

        // Act
        startGate.SetResult();
        await source.FetchStarted.WaitAsync(TimeSpan.FromSeconds(10));
        await allReusedCompleted.Task.WaitAsync(
            TimeSpan.FromSeconds(10));
        source.Release();
        var results = await Task.WhenAll(scanTasks);

        // Assert
        Assert.Equal(1, source.FetchCount);
        Assert.Single(results.Select(result => result.ScanRunId).Distinct());
        Assert.Equal(7, results.Count(result =>
            result.ReusedExistingRun));

        await using var assertContext = _fixture.CreateDbContext();
        Assert.Equal(1, await assertContext.ScanRuns.CountAsync());
        Assert.Equal(
            ScanRunStatus.Succeeded,
            await assertContext.ScanRuns
                .Select(scan => scan.Status)
                .SingleAsync());
    }

    [Fact]
    public async Task ScanAsync_WithExpiredLease_StartsNewScan()
    {
        // Arrange
        await using var arrangeContext = _fixture.CreateDbContext();
        var course = new ExternalCourse(
            new ExternalCourseIdentity(
                "mock-moodle",
                "https://moodle.example.test",
                "course-17"),
            "Software Engineering",
            DateTimeOffset.UnixEpoch);
        course.Activate();
        var expiredScan = new ScanRun(
            course.Id,
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch.AddMinutes(5));
        arrangeContext.ExternalCourses.Add(course);
        arrangeContext.ScanRuns.Add(expiredScan);
        await arrangeContext.SaveChangesAsync();

        var source = new StubExternalCourseSource(
            new ExternalCourseSourcePayload([]));
        var orchestrator = new CourseScanOrchestrator(
            new TestDbContextFactory(_fixture),
            source,
            new FixedTimeProvider(
                DateTimeOffset.UnixEpoch.AddMinutes(10)),
            new CourseScanOptions(
                TimeSpan.FromMinutes(5),
                TimeSpan.FromSeconds(30)));

        // Act
        var result = await orchestrator.ScanAsync(course.Id);

        // Assert
        Assert.Equal(ScanRunStatus.Succeeded, result.Status);
        Assert.False(result.ReusedExistingRun);
        Assert.Equal(1, source.FetchCount);

        await using var assertContext = _fixture.CreateDbContext();
        Assert.Equal(2, await assertContext.ScanRuns.CountAsync());
        var savedExpiredScan = await assertContext.ScanRuns
            .SingleAsync(scan => scan.Id == expiredScan.Id);
        Assert.Equal(ScanRunStatus.Expired, savedExpiredScan.Status);
        Assert.Equal(
            ScanRunErrorCode.Timeout,
            savedExpiredScan.ErrorCode);
    }

    [Fact]
    public async Task ScanAsync_ForPendingActivation_ActivatesSubscription()
    {
        // Arrange
        await using var arrangeContext = _fixture.CreateDbContext();
        var user = CreateUser();
        var module = new StudyModule(user.Id, "Software Engineering");
        var course = new ExternalCourse(
            new ExternalCourseIdentity(
                "mock-moodle",
                "https://moodle.example.test",
                "course-17"),
            "Software Engineering",
            DateTimeOffset.UnixEpoch);
        var subscription = new CourseSubscription(
            module.Id,
            user.Id,
            course.Id,
            DateTimeOffset.UnixEpoch);

        arrangeContext.Users.Add(user);
        arrangeContext.Modules.Add(module);
        arrangeContext.ExternalCourses.Add(course);
        arrangeContext.CourseSubscriptions.Add(subscription);
        await arrangeContext.SaveChangesAsync();

        var source = new StubExternalCourseSource(
            CreatePayload("Exercise sheet", null));
        var now = DateTimeOffset.UnixEpoch.AddHours(1);
        var orchestrator = new CourseScanOrchestrator(
            new TestDbContextFactory(_fixture),
            source,
            new FixedTimeProvider(now),
            new CourseScanOptions(
                TimeSpan.FromMinutes(5),
                TimeSpan.FromSeconds(30)));

        // Act
        var result = await orchestrator.ScanAsync(
            course.Id,
            subscription.Id);

        // Assert
        Assert.Equal(ScanRunStatus.Succeeded, result.Status);

        await using var assertContext = _fixture.CreateDbContext();
        var savedCourse = await assertContext.ExternalCourses.SingleAsync();
        var savedSubscription =
            await assertContext.CourseSubscriptions.SingleAsync();
        var savedScan = await assertContext.ScanRuns.SingleAsync();

        Assert.Equal(ExternalCourseState.Active, savedCourse.State);
        Assert.Equal(
            CourseSubscriptionState.Active,
            savedSubscription.State);
        Assert.Equal(now, savedSubscription.ActivatedAt);
        Assert.Null(savedScan.ActivationSubscriptionId);
        Assert.Equal(1, await assertContext.Tasks.CountAsync());
        Assert.Equal(
            1,
            await assertContext.SubscriptionContentStates.CountAsync());
    }

    [Fact]
    public async Task ScanAsync_WhenPersistenceFails_RollsBackImport()
    {
        // Arrange
        await using var arrangeContext = _fixture.CreateDbContext();
        var course = new ExternalCourse(
            new ExternalCourseIdentity(
                "mock-moodle",
                "https://moodle.example.test",
                "course-17"),
            "Software Engineering",
            DateTimeOffset.UnixEpoch);
        course.Activate();
        arrangeContext.ExternalCourses.Add(course);
        await arrangeContext.SaveChangesAsync();

        var source = new StubExternalCourseSource(
            CreatePayload("Exercise sheet", null));
        var orchestrator = new CourseScanOrchestrator(
            new FaultingPersistenceDbContextFactory(_fixture),
            source,
            new FixedTimeProvider(
                DateTimeOffset.UnixEpoch.AddHours(1)),
            new CourseScanOptions(
                TimeSpan.FromMinutes(5),
                TimeSpan.FromSeconds(30)));

        // Act
        var result = await orchestrator.ScanAsync(course.Id);

        // Assert
        Assert.Equal(ScanRunStatus.Failed, result.Status);
        Assert.Equal(
            ScanRunErrorCode.PersistenceConflict,
            result.ErrorCode);

        await using var assertContext = _fixture.CreateDbContext();
        Assert.Empty(await assertContext.CourseSnapshots.ToListAsync());
        Assert.Empty(
            await assertContext.ExternalLearningContents.ToListAsync());
        Assert.Empty(await assertContext.Tasks.ToListAsync());
        var savedScan = await assertContext.ScanRuns.SingleAsync();
        Assert.Equal(ScanRunStatus.Failed, savedScan.Status);
        Assert.Equal(
            ScanRunErrorCode.PersistenceConflict,
            savedScan.ErrorCode);
    }

    [Fact]
    public async Task ScanAsync_WithDismissedImport_DoesNotRecreateTask()
    {
        // Arrange
        await using var arrangeContext = _fixture.CreateDbContext();
        var user = CreateUser();
        var module = new StudyModule(user.Id, "Software Engineering");
        var course = new ExternalCourse(
            new ExternalCourseIdentity(
                "mock-moodle",
                "https://moodle.example.test",
                "course-17"),
            "Software Engineering",
            DateTimeOffset.UnixEpoch);
        course.Activate();
        var subscription = new CourseSubscription(
            module.Id,
            user.Id,
            course.Id,
            DateTimeOffset.UnixEpoch);
        subscription.Activate(DateTimeOffset.UnixEpoch);

        arrangeContext.Users.Add(user);
        arrangeContext.Modules.Add(module);
        arrangeContext.ExternalCourses.Add(course);
        arrangeContext.CourseSubscriptions.Add(subscription);
        await arrangeContext.SaveChangesAsync();

        var source = new StubExternalCourseSource(
            CreatePayload("Exercise sheet", null));
        var orchestrator = new CourseScanOrchestrator(
            new TestDbContextFactory(_fixture),
            source,
            new FixedTimeProvider(
                DateTimeOffset.UnixEpoch.AddHours(1)),
            new CourseScanOptions(
                TimeSpan.FromMinutes(5),
                TimeSpan.FromSeconds(30)));
        await orchestrator.ScanAsync(course.Id);

        await using (var dismissContext = _fixture.CreateDbContext())
        await using (var transaction = await dismissContext.Database
            .BeginTransactionAsync())
        {
            var state =
                await dismissContext.SubscriptionContentStates
                    .SingleAsync();
            state.Dismiss(DateTimeOffset.UnixEpoch.AddHours(2));
            await dismissContext.SaveChangesAsync();
            var task = await dismissContext.Tasks.SingleAsync();
            dismissContext.Tasks.Remove(task);
            await dismissContext.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        source.UsePayload(
            CreatePayload("Renamed exercise sheet", null));

        // Act
        await orchestrator.ScanAsync(course.Id);

        // Assert
        await using var assertContext = _fixture.CreateDbContext();
        Assert.Empty(await assertContext.Tasks.ToListAsync());
        Assert.Empty(await assertContext.SourceUpdates.ToListAsync());
        var savedState =
            await assertContext.SubscriptionContentStates.SingleAsync();
        Assert.Equal(
            SubscriptionContentStateStatus.Dismissed,
            savedState.Status);
        Assert.Null(savedState.StudyTaskId);
    }

    [Fact]
    public async Task ScanAsync_WithTwoSubscribers_ImportsEachContentOnce()
    {
        // Arrange
        await using var arrangeContext = _fixture.CreateDbContext();
        var firstUser = CreateUser("first@example.test");
        var secondUser = CreateUser("second@example.test");
        var firstModule = new StudyModule(firstUser.Id, "Module A");
        var secondModule = new StudyModule(secondUser.Id, "Module B");
        var course = new ExternalCourse(
            new ExternalCourseIdentity(
                "mock-moodle",
                "https://moodle.example.test",
                "course-17"),
            "Software Engineering",
            DateTimeOffset.UnixEpoch);
        course.Activate();
        var firstSubscription = new CourseSubscription(
            firstModule.Id,
            firstUser.Id,
            course.Id,
            DateTimeOffset.UnixEpoch);
        firstSubscription.Activate(DateTimeOffset.UnixEpoch);
        var secondSubscription = new CourseSubscription(
            secondModule.Id,
            secondUser.Id,
            course.Id,
            DateTimeOffset.UnixEpoch);
        secondSubscription.Activate(DateTimeOffset.UnixEpoch);

        arrangeContext.Users.AddRange(firstUser, secondUser);
        arrangeContext.Modules.AddRange(firstModule, secondModule);
        arrangeContext.ExternalCourses.Add(course);
        arrangeContext.CourseSubscriptions.AddRange(
            firstSubscription,
            secondSubscription);
        await arrangeContext.SaveChangesAsync();

        var source = new StubExternalCourseSource(
            new ExternalCourseSourcePayload(
            [
                new CourseSourceItem(
                    new ExternalContentKey("file-17"),
                    ExternalLearningContentType.File,
                    "Exercise sheet",
                    null,
                    "application/pdf",
                    "/mod/resource/17"),
                new CourseSourceItem(
                    new ExternalContentKey("link-18"),
                    ExternalLearningContentType.Link,
                    "Reference",
                    null,
                    null,
                    null),
                new CourseSourceItem(
                    new ExternalContentKey("activity-19"),
                    ExternalLearningContentType.Activity,
                    "Quiz",
                    DateTimeOffset.UnixEpoch.AddDays(3),
                    null,
                    "/mod/quiz/19")
            ]));
        var orchestrator = new CourseScanOrchestrator(
            new TestDbContextFactory(_fixture),
            source,
            new FixedTimeProvider(
                DateTimeOffset.UnixEpoch.AddHours(1)),
            new CourseScanOptions(
                TimeSpan.FromMinutes(5),
                TimeSpan.FromSeconds(30)));

        // Act
        var result = await orchestrator.ScanAsync(course.Id);

        // Assert
        Assert.Equal(ScanRunStatus.Succeeded, result.Status);
        Assert.Equal(3, result.Counts.New);

        await using var assertContext = _fixture.CreateDbContext();
        Assert.Equal(
            3,
            await assertContext.ExternalLearningContents.CountAsync());
        Assert.Equal(
            3,
            await assertContext.CourseSnapshotItems.CountAsync());
        Assert.Equal(6, await assertContext.Tasks.CountAsync());
        Assert.Equal(
            6,
            await assertContext.SubscriptionContentStates.CountAsync());
        Assert.Equal(
            3,
            await assertContext.Tasks.CountAsync(task =>
                task.ModuleId == firstModule.Id));
        Assert.Equal(
            3,
            await assertContext.Tasks.CountAsync(task =>
                task.ModuleId == secondModule.Id));
    }

    [Fact]
    public async Task ScanAsync_ForDifferentCourses_RunsInParallel()
    {
        // Arrange
        await using var arrangeContext = _fixture.CreateDbContext();
        var firstCourse = new ExternalCourse(
            new ExternalCourseIdentity(
                "mock-moodle",
                "https://moodle.example.test",
                "course-17"),
            "Course A",
            DateTimeOffset.UnixEpoch);
        var secondCourse = new ExternalCourse(
            new ExternalCourseIdentity(
                "mock-moodle",
                "https://moodle.example.test",
                "course-18"),
            "Course B",
            DateTimeOffset.UnixEpoch);
        firstCourse.Activate();
        secondCourse.Activate();
        arrangeContext.ExternalCourses.AddRange(
            firstCourse,
            secondCourse);
        await arrangeContext.SaveChangesAsync();

        var source = new TwoCourseBlockingSource();
        var orchestrator = new CourseScanOrchestrator(
            new TestDbContextFactory(_fixture),
            source,
            new FixedTimeProvider(
                DateTimeOffset.UnixEpoch.AddHours(1)),
            new CourseScanOptions(
                TimeSpan.FromMinutes(5),
                TimeSpan.FromSeconds(30)));

        // Act
        var firstScan = orchestrator.ScanAsync(firstCourse.Id);
        var secondScan = orchestrator.ScanAsync(secondCourse.Id);
        await source.BothFetchesStarted.WaitAsync(
            TimeSpan.FromSeconds(10));
        source.Release();
        var results = await Task.WhenAll(firstScan, secondScan);

        // Assert
        Assert.All(results, result =>
            Assert.Equal(ScanRunStatus.Succeeded, result.Status));
        Assert.Equal(2, source.MaximumConcurrency);
        Assert.Equal(
            2,
            results.Select(result => result.ScanRunId).Distinct().Count());

        await using var assertContext = _fixture.CreateDbContext();
        Assert.Equal(2, await assertContext.ScanRuns.CountAsync());
    }

    [Fact]
    public async Task ScanAsync_WhenSourceDeniesAccess_ReturnsStableFailure()
    {
        // Arrange
        await using var arrangeContext = _fixture.CreateDbContext();
        var identity = new ExternalCourseIdentity(
            "mock-moodle",
            "https://moodle.example.test",
            "course-17");
        var course = new ExternalCourse(
            identity,
            "Software Engineering",
            DateTimeOffset.UnixEpoch);
        course.Activate();
        arrangeContext.ExternalCourses.Add(course);
        await arrangeContext.SaveChangesAsync();

        var source = new MockExternalCourseSource();
        source.RegisterCourse(
            identity,
            "initial",
            new Dictionary<string, ExternalCourseSourcePayload>
            {
                ["initial"] = new ExternalCourseSourcePayload([])
            });
        source.FailWith(identity, ScanRunErrorCode.AccessDenied);
        var orchestrator = new CourseScanOrchestrator(
            new TestDbContextFactory(_fixture),
            source,
            new FixedTimeProvider(
                DateTimeOffset.UnixEpoch.AddHours(1)),
            new CourseScanOptions(
                TimeSpan.FromMinutes(5),
                TimeSpan.FromSeconds(30)));

        // Act
        var result = await orchestrator.ScanAsync(course.Id);

        // Assert
        Assert.Equal(ScanRunStatus.Failed, result.Status);
        Assert.Equal(ScanRunErrorCode.AccessDenied, result.ErrorCode);
        Assert.Equal(0, result.Counts.New);
        Assert.Equal(0, result.Counts.Updated);
        Assert.Equal(0, result.Counts.Unchanged);
        Assert.Equal(0, result.Counts.Unavailable);

        await using var assertContext = _fixture.CreateDbContext();
        Assert.Empty(await assertContext.CourseSnapshots.ToListAsync());
        var savedScan = await assertContext.ScanRuns.SingleAsync();
        Assert.Equal(ScanRunStatus.Failed, savedScan.Status);
        Assert.Equal(ScanRunErrorCode.AccessDenied, savedScan.ErrorCode);
    }

    [Fact]
    public async Task ScanAsync_WhenCancelledDuringFetch_MarksRunCancelled()
    {
        // Arrange
        await using var arrangeContext = _fixture.CreateDbContext();
        var course = new ExternalCourse(
            new ExternalCourseIdentity(
                "mock-moodle",
                "https://moodle.example.test",
                "course-17"),
            "Software Engineering",
            DateTimeOffset.UnixEpoch);
        course.Activate();
        arrangeContext.ExternalCourses.Add(course);
        await arrangeContext.SaveChangesAsync();

        var source = new BlockingExternalCourseSource(
            new ExternalCourseSourcePayload([]));
        var orchestrator = new CourseScanOrchestrator(
            new TestDbContextFactory(_fixture),
            source,
            new FixedTimeProvider(
                DateTimeOffset.UnixEpoch.AddHours(1)),
            new CourseScanOptions(
                TimeSpan.FromMinutes(5),
                TimeSpan.FromSeconds(30)));
        using var cancellation = new CancellationTokenSource();

        // Act
        var scanTask = orchestrator.ScanAsync(
            course.Id,
            cancellationToken: cancellation.Token);
        await source.FetchStarted.WaitAsync(TimeSpan.FromSeconds(10));
        cancellation.Cancel();
        var result = await scanTask;

        // Assert
        Assert.Equal(ScanRunStatus.Cancelled, result.Status);
        Assert.Null(result.ErrorCode);

        await using var assertContext = _fixture.CreateDbContext();
        Assert.Empty(await assertContext.CourseSnapshots.ToListAsync());
        var savedScan = await assertContext.ScanRuns.SingleAsync();
        Assert.Equal(ScanRunStatus.Cancelled, savedScan.Status);
        Assert.Null(savedScan.ErrorCode);
    }

    [Fact]
    public async Task ScanAsync_WhenSourceThrowsUnexpected_ReturnsStableFailure()
    {
        // Arrange
        await using var arrangeContext = _fixture.CreateDbContext();
        var course = new ExternalCourse(
            new ExternalCourseIdentity(
                "mock-moodle",
                "https://moodle.example.test",
                "course-17"),
            "Software Engineering",
            DateTimeOffset.UnixEpoch);
        course.Activate();
        arrangeContext.ExternalCourses.Add(course);
        await arrangeContext.SaveChangesAsync();

        var source = new StubExternalCourseSource(
            new ExternalCourseSourcePayload([]));
        source.UseUnexpectedFailure();
        var orchestrator = new CourseScanOrchestrator(
            new TestDbContextFactory(_fixture),
            source,
            new FixedTimeProvider(
                DateTimeOffset.UnixEpoch.AddHours(1)),
            new CourseScanOptions(
                TimeSpan.FromMinutes(5),
                TimeSpan.FromSeconds(30)));

        // Act
        var result = await orchestrator.ScanAsync(course.Id);

        // Assert
        Assert.Equal(ScanRunStatus.Failed, result.Status);
        Assert.Equal(ScanRunErrorCode.Unexpected, result.ErrorCode);

        await using var assertContext = _fixture.CreateDbContext();
        Assert.Empty(await assertContext.CourseSnapshots.ToListAsync());
        var savedScan = await assertContext.ScanRuns.SingleAsync();
        Assert.Equal(ScanRunErrorCode.Unexpected, savedScan.ErrorCode);
    }

    [Fact]
    public async Task ScanAsync_WhenLeaseExpiresDuringFetch_DoesNotPublishStaleResult()
    {
        // Arrange
        await using var arrangeContext = _fixture.CreateDbContext();
        var course = new ExternalCourse(
            new ExternalCourseIdentity(
                "mock-moodle",
                "https://moodle.example.test",
                "course-17"),
            "Software Engineering",
            DateTimeOffset.UnixEpoch);
        course.Activate();
        arrangeContext.ExternalCourses.Add(course);
        await arrangeContext.SaveChangesAsync();

        var source = new SequencedExternalCourseSource(
            CreatePayload("Stale exercise", null),
            CreatePayload("Fresh exercise", null));
        var clock = new MutableTimeProvider(
            DateTimeOffset.UnixEpoch.AddHours(1));
        var orchestrator = new CourseScanOrchestrator(
            new TestDbContextFactory(_fixture),
            source,
            clock,
            new CourseScanOptions(
                TimeSpan.FromMinutes(5),
                TimeSpan.FromSeconds(30)));

        var staleScanTask = orchestrator.ScanAsync(course.Id);
        await source.FirstFetchStarted.WaitAsync(
            TimeSpan.FromSeconds(10));
        clock.SetUtcNow(
            DateTimeOffset.UnixEpoch
                .AddHours(1)
                .AddMinutes(6));

        var freshResult = await orchestrator.ScanAsync(course.Id);

        // Act
        source.ReleaseFirstFetch();
        var staleResult = await staleScanTask;

        // Assert
        Assert.Equal(ScanRunStatus.Succeeded, freshResult.Status);
        Assert.Equal(ScanRunStatus.Expired, staleResult.Status);

        await using var assertContext = _fixture.CreateDbContext();
        Assert.Equal(1, await assertContext.CourseSnapshots.CountAsync());
        Assert.Equal(
            "Fresh exercise",
            await assertContext.ExternalLearningContents
                .Select(content => content.Title)
                .SingleAsync());
        Assert.Equal(
            1,
            await assertContext.ScanRuns.CountAsync(scan =>
                scan.Status == ScanRunStatus.Expired));
        Assert.Equal(
            1,
            await assertContext.ScanRuns.CountAsync(scan =>
                scan.Status == ScanRunStatus.Succeeded));
    }

    [Fact]
    public async Task ScanAsync_WhenExpiredStaleFetchFails_PreservesExpiredRun()
    {
        // Arrange
        await using var arrangeContext = _fixture.CreateDbContext();
        var course = new ExternalCourse(
            new ExternalCourseIdentity(
                "mock-moodle",
                "https://moodle.example.test",
                "course-17"),
            "Software Engineering",
            DateTimeOffset.UnixEpoch);
        course.Activate();
        arrangeContext.ExternalCourses.Add(course);
        await arrangeContext.SaveChangesAsync();

        var source = new SequencedExternalCourseSource(
            CreatePayload("Stale exercise", null),
            CreatePayload("Fresh exercise", null),
            firstFetchFails: true);
        var clock = new MutableTimeProvider(
            DateTimeOffset.UnixEpoch.AddHours(1));
        var orchestrator = new CourseScanOrchestrator(
            new TestDbContextFactory(_fixture),
            source,
            clock,
            new CourseScanOptions(
                TimeSpan.FromMinutes(5),
                TimeSpan.FromSeconds(30)));

        var staleScanTask = orchestrator.ScanAsync(course.Id);
        await source.FirstFetchStarted.WaitAsync(
            TimeSpan.FromSeconds(10));
        clock.SetUtcNow(
            DateTimeOffset.UnixEpoch
                .AddHours(1)
                .AddMinutes(6));
        var freshResult = await orchestrator.ScanAsync(course.Id);

        // Act
        source.ReleaseFirstFetch();
        var staleResult = await staleScanTask;

        // Assert
        Assert.Equal(ScanRunStatus.Succeeded, freshResult.Status);
        Assert.Equal(ScanRunStatus.Expired, staleResult.Status);
        Assert.Equal(ScanRunErrorCode.Timeout, staleResult.ErrorCode);

        await using var assertContext = _fixture.CreateDbContext();
        Assert.Equal(
            1,
            await assertContext.ScanRuns.CountAsync(scan =>
                scan.Status == ScanRunStatus.Expired));
        Assert.Equal(
            1,
            await assertContext.ScanRuns.CountAsync(scan =>
                scan.Status == ScanRunStatus.Succeeded));
    }

    [Fact]
    public async Task ScanAsync_WhenOwnLeaseExpires_DoesNotPublishSnapshot()
    {
        // Arrange
        await using var arrangeContext = _fixture.CreateDbContext();
        var course = new ExternalCourse(
            new ExternalCourseIdentity(
                "mock-moodle",
                "https://moodle.example.test",
                "course-17"),
            "Software Engineering",
            DateTimeOffset.UnixEpoch);
        course.Activate();
        arrangeContext.ExternalCourses.Add(course);
        await arrangeContext.SaveChangesAsync();

        var source = new BlockingExternalCourseSource(
            new ExternalCourseSourcePayload([]));
        var clock = new MutableTimeProvider(
            DateTimeOffset.UnixEpoch.AddHours(1));
        var orchestrator = new CourseScanOrchestrator(
            new TestDbContextFactory(_fixture),
            source,
            clock,
            new CourseScanOptions(
                TimeSpan.FromMinutes(5),
                TimeSpan.FromSeconds(30)));

        var scanTask = orchestrator.ScanAsync(course.Id);
        await source.FetchStarted.WaitAsync(TimeSpan.FromSeconds(10));
        clock.SetUtcNow(
            DateTimeOffset.UnixEpoch
                .AddHours(1)
                .AddMinutes(6));

        // Act
        source.Release();
        var result = await scanTask;

        // Assert
        Assert.Equal(ScanRunStatus.Expired, result.Status);
        Assert.Equal(ScanRunErrorCode.Timeout, result.ErrorCode);

        await using var assertContext = _fixture.CreateDbContext();
        Assert.Empty(await assertContext.CourseSnapshots.ToListAsync());
        Assert.Equal(
            ScanRunStatus.Expired,
            await assertContext.ScanRuns
                .Select(scan => scan.Status)
                .SingleAsync());
    }

    [Fact]
    public async Task ScanAsync_WhenActivationEndsDuringFetch_DoesNotPublish()
    {
        // Arrange
        await using var arrangeContext = _fixture.CreateDbContext();
        var user = CreateUser();
        var module = new StudyModule(user.Id, "Software Engineering");
        var course = new ExternalCourse(
            new ExternalCourseIdentity(
                "mock-moodle",
                "https://moodle.example.test",
                "course-17"),
            "Software Engineering",
            DateTimeOffset.UnixEpoch);
        var subscription = new CourseSubscription(
            module.Id,
            user.Id,
            course.Id,
            DateTimeOffset.UnixEpoch);
        arrangeContext.Users.Add(user);
        arrangeContext.Modules.Add(module);
        arrangeContext.ExternalCourses.Add(course);
        arrangeContext.CourseSubscriptions.Add(subscription);
        await arrangeContext.SaveChangesAsync();

        var source = new BlockingExternalCourseSource(
            CreatePayload("Exercise sheet", null));
        var orchestrator = new CourseScanOrchestrator(
            new TestDbContextFactory(_fixture),
            source,
            new FixedTimeProvider(
                DateTimeOffset.UnixEpoch.AddHours(1)),
            new CourseScanOptions(
                TimeSpan.FromMinutes(5),
                TimeSpan.FromSeconds(30)));

        var scanTask = orchestrator.ScanAsync(
            course.Id,
            subscription.Id);
        await source.FetchStarted.WaitAsync(TimeSpan.FromSeconds(10));

        await using (var endContext = _fixture.CreateDbContext())
        {
            var savedSubscription =
                await endContext.CourseSubscriptions.SingleAsync();
            savedSubscription.End(
                DateTimeOffset.UnixEpoch.AddHours(1));
            await endContext.SaveChangesAsync();
        }

        // Act
        source.Release();
        var result = await scanTask;

        // Assert
        Assert.Equal(ScanRunStatus.Cancelled, result.Status);

        await using var assertContext = _fixture.CreateDbContext();
        Assert.Equal(
            ExternalCourseState.Inactive,
            await assertContext.ExternalCourses
                .Select(item => item.State)
                .SingleAsync());
        Assert.Equal(
            CourseSubscriptionState.Ended,
            await assertContext.CourseSubscriptions
                .Select(item => item.State)
                .SingleAsync());
        Assert.Empty(await assertContext.CourseSnapshots.ToListAsync());
        Assert.Empty(
            await assertContext.ExternalLearningContents.ToListAsync());
        Assert.Empty(await assertContext.Tasks.ToListAsync());
    }

    private static ExternalCourseSourcePayload CreatePayload(
        string title,
        DateTimeOffset? dueDate)
    {
        return new ExternalCourseSourcePayload(
        [
            new CourseSourceItem(
                new ExternalContentKey("file-17"),
                ExternalLearningContentType.File,
                title,
                dueDate,
                "application/pdf",
                "/mod/resource/17")
        ]);
    }

    private static ApplicationUser CreateUser(
        string email = "student@example.test")
    {
        return new ApplicationUser
        {
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant()
        };
    }

    private sealed class TestDbContextFactory(
        PostgreSqlFixture fixture)
        : IDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext()
        {
            return fixture.CreateDbContext();
        }
    }

    private sealed class FaultingPersistenceDbContextFactory(
        PostgreSqlFixture fixture)
        : IDbContextFactory<ApplicationDbContext>
    {
        private int _createdContextCount;

        public ApplicationDbContext CreateDbContext()
        {
            var contextNumber =
                Interlocked.Increment(ref _createdContextCount);
            return contextNumber == 2
                ? fixture.CreateDbContext(
                    new ThrowingSaveChangesInterceptor())
                : fixture.CreateDbContext();
        }
    }

    private sealed class ThrowingSaveChangesInterceptor
        : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>>
            SavingChangesAsync(
                DbContextEventData eventData,
                InterceptionResult<int> result,
                CancellationToken cancellationToken = default)
        {
            throw new DbUpdateException(
                "Simulated persistence conflict.");
        }
    }

    private sealed class StubExternalCourseSource
        : IExternalCourseSource
    {
        private ExternalCourseSourcePayload _sourcePayload;
        private bool _timesOut;
        private bool _throwsUnexpected;
        private int _fetchCount;

        public int FetchCount => _fetchCount;

        public StubExternalCourseSource(
            ExternalCourseSourcePayload sourcePayload)
        {
            _sourcePayload = sourcePayload;
        }

        public Task<ExternalCourseSourcePayload> FetchCourseDataAsync(
            ExternalCourseIdentity identity,
            CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _fetchCount);

            if (_timesOut)
            {
                return Task.FromException<ExternalCourseSourcePayload>(
                    new TimeoutException());
            }

            if (_throwsUnexpected)
            {
                return Task.FromException<ExternalCourseSourcePayload>(
                    new InvalidOperationException(
                        "Sensitive adapter detail."));
            }

            return Task.FromResult(_sourcePayload);
        }

        public void UsePayload(ExternalCourseSourcePayload sourcePayload)
        {
            _sourcePayload = sourcePayload;
            _timesOut = false;
            _throwsUnexpected = false;
        }

        public void UseTimeout()
        {
            _timesOut = true;
        }

        public void UseUnexpectedFailure()
        {
            _throwsUnexpected = true;
        }
    }

    private sealed class BlockingExternalCourseSource(
        ExternalCourseSourcePayload sourcePayload)
        : IExternalCourseSource
    {
        private readonly TaskCompletionSource _fetchStarted = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _release = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        private int _fetchCount;

        public Task FetchStarted => _fetchStarted.Task;

        public int FetchCount => _fetchCount;

        public async Task<ExternalCourseSourcePayload> FetchCourseDataAsync(
            ExternalCourseIdentity identity,
            CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _fetchCount);
            _fetchStarted.TrySetResult();
            await _release.Task.WaitAsync(cancellationToken);
            return sourcePayload;
        }

        public void Release()
        {
            _release.TrySetResult();
        }
    }

    private sealed class TwoCourseBlockingSource
        : IExternalCourseSource
    {
        private readonly TaskCompletionSource _bothFetchesStarted = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _release = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        private int _activeFetches;
        private int _maximumConcurrency;

        public Task BothFetchesStarted => _bothFetchesStarted.Task;

        public int MaximumConcurrency => _maximumConcurrency;

        public async Task<ExternalCourseSourcePayload> FetchCourseDataAsync(
            ExternalCourseIdentity identity,
            CancellationToken cancellationToken = default)
        {
            var activeFetches = Interlocked.Increment(
                ref _activeFetches);
            UpdateMaximumConcurrency(activeFetches);
            if (activeFetches == 2)
            {
                _bothFetchesStarted.TrySetResult();
            }

            try
            {
                await _release.Task.WaitAsync(cancellationToken);
                return new ExternalCourseSourcePayload([]);
            }
            finally
            {
                Interlocked.Decrement(ref _activeFetches);
            }
        }

        public void Release()
        {
            _release.TrySetResult();
        }

        private void UpdateMaximumConcurrency(int candidate)
        {
            var current = Volatile.Read(ref _maximumConcurrency);
            while (candidate > current)
            {
                var previous = Interlocked.CompareExchange(
                    ref _maximumConcurrency,
                    candidate,
                    current);
                if (previous == current)
                {
                    return;
                }

                current = previous;
            }
        }
    }

    private sealed class FixedTimeProvider(
        DateTimeOffset utcNow)
        : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return utcNow;
        }
    }

    private sealed class MutableTimeProvider(
        DateTimeOffset utcNow)
        : TimeProvider
    {
        private DateTimeOffset _utcNow = utcNow;

        public override DateTimeOffset GetUtcNow()
        {
            return _utcNow;
        }

        public void SetUtcNow(DateTimeOffset value)
        {
            _utcNow = value;
        }
    }

    private sealed class SequencedExternalCourseSource(
        ExternalCourseSourcePayload firstSourcePayload,
        ExternalCourseSourcePayload secondSourcePayload,
        bool firstFetchFails = false)
        : IExternalCourseSource
    {
        private readonly TaskCompletionSource _firstFetchStarted = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _releaseFirstFetch = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        private int _fetchCount;

        public Task FirstFetchStarted => _firstFetchStarted.Task;

        public async Task<ExternalCourseSourcePayload> FetchCourseDataAsync(
            ExternalCourseIdentity identity,
            CancellationToken cancellationToken = default)
        {
            var fetchNumber = Interlocked.Increment(ref _fetchCount);
            if (fetchNumber == 1)
            {
                _firstFetchStarted.TrySetResult();
                await _releaseFirstFetch.Task.WaitAsync(
                    cancellationToken);
                if (firstFetchFails)
                {
                    throw new InvalidOperationException(
                        "Stale source failure.");
                }

                return firstSourcePayload;
            }

            return secondSourcePayload;
        }

        public void ReleaseFirstFetch()
        {
            _releaseFirstFetch.TrySetResult();
        }
    }
}
