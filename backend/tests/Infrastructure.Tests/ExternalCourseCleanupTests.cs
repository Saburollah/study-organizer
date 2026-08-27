using Microsoft.EntityFrameworkCore;
using Npgsql;
using StudyOrganizer.Application.Tasks;
using StudyOrganizer.Domain.ExternalCourses;
using StudyOrganizer.Infrastructure.ExternalCourses;
using StudyOrganizer.Infrastructure.Identity;
using StudyOrganizer.Infrastructure.Persistence;
using StudyOrganizer.Infrastructure.Tasks;

namespace StudyOrganizer.Infrastructure.Tests;

public sealed class ExternalCourseCleanupTests
    : IClassFixture<PostgreSqlFixture>, IAsyncLifetime
{
    private readonly PostgreSqlFixture _fixture;

    public ExternalCourseCleanupTests(PostgreSqlFixture fixture)
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
    public async Task CleanupExpiredAsync_BeforeRetentionPeriod_DoesNothing()
    {
        // Arrange
        var inactiveAt = DateTimeOffset.UnixEpoch;
        await SeedInactiveCourseAsync(inactiveAt);
        var cleanup = CreateCleanup(
            inactiveAt.AddDays(30).AddTicks(-1));

        // Act
        var cleanedCourseCount = await cleanup.CleanupExpiredAsync();

        // Assert
        Assert.Equal(0, cleanedCourseCount);
        await using var context = _fixture.CreateDbContext();
        Assert.Equal(1, await context.ExternalCourses.CountAsync());
        Assert.Equal(1, await context.CourseSubscriptions.CountAsync());
        Assert.Equal(1, await context.ExternalLearningContents.CountAsync());
        Assert.Equal(1, await context.ScanRuns.CountAsync());
        Assert.Equal(1, await context.CourseSnapshots.CountAsync());
        Assert.Equal(1, await context.CourseSnapshotItems.CountAsync());
    }

    [Fact]
    public async Task CleanupExpiredAsync_WithoutPersonalReferences_DeletesCourseGraphIdempotently()
    {
        // Arrange
        var inactiveAt = DateTimeOffset.UnixEpoch;
        await SeedInactiveCourseAsync(inactiveAt);
        var cleanup = CreateCleanup(inactiveAt.AddDays(30));

        // Act
        var firstCleanedCourseCount = await cleanup.CleanupExpiredAsync();
        var secondCleanedCourseCount = await cleanup.CleanupExpiredAsync();

        // Assert
        Assert.Equal(1, firstCleanedCourseCount);
        Assert.Equal(0, secondCleanedCourseCount);
        await using var context = _fixture.CreateDbContext();
        Assert.Equal(0, await context.ExternalCourses.CountAsync());
        Assert.Equal(0, await context.CourseSubscriptions.CountAsync());
        Assert.Equal(0, await context.ExternalLearningContents.CountAsync());
        Assert.Equal(0, await context.ScanRuns.CountAsync());
        Assert.Equal(0, await context.CourseSnapshots.CountAsync());
        Assert.Equal(0, await context.CourseSnapshotItems.CountAsync());
    }

    [Fact]
    public async Task CleanupExpiredAsync_WithPersonalReference_PurgesOnlyExternalMetadata()
    {
        // Arrange
        var inactiveAt = DateTimeOffset.UnixEpoch;
        var graph = await SeedInactiveCourseAsync(
            inactiveAt,
            withPersonalReference: true);
        var cleanupAt = inactiveAt.AddDays(30);
        var cleanup = CreateCleanup(cleanupAt);

        // Act
        var firstCleanedCourseCount = await cleanup.CleanupExpiredAsync();
        var secondCleanedCourseCount = await cleanup.CleanupExpiredAsync();

        // Assert
        Assert.Equal(1, firstCleanedCourseCount);
        Assert.Equal(0, secondCleanedCourseCount);
        await using var context = _fixture.CreateDbContext();
        Assert.Equal(1, await context.ExternalCourses.CountAsync());
        Assert.Equal(1, await context.CourseSubscriptions.CountAsync());
        Assert.Equal(1, await context.SubscriptionContentStates.CountAsync());
        Assert.Equal(1, await context.Tasks.CountAsync());
        Assert.Equal(0, await context.ScanRuns.CountAsync());
        Assert.Equal(0, await context.CourseSnapshots.CountAsync());
        Assert.Equal(0, await context.CourseSnapshotItems.CountAsync());

        var content = await context.ExternalLearningContents.SingleAsync();
        Assert.Equal(graph.ContentId, content.Id);
        Assert.Equal("file-17", content.ExternalContentKey.Value);
        Assert.Equal(cleanupAt, content.MetadataPurgedAt);
        Assert.Null(content.DueDate);
        Assert.Null(content.MediaType);
        Assert.Null(content.SourceReference);

        await using var taskContext = _fixture.CreateDbContext();
        var taskHandler = new StudyTaskHandler(
            taskContext,
            new MockMoodleCourseUrlResolver(),
            new FixedTimeProvider(cleanupAt));
        var tasks = await taskHandler.GetByModuleAsync(
            graph.OwnerId,
            graph.ModuleId);
        var task = Assert.Single(tasks ?? []);
        Assert.Equal(
            StudyTaskImportSourceStatus.MetadataPurged,
            task.ImportSource?.Status);
        Assert.Null(task.ImportSource?.ContentType);
        Assert.Null(task.ImportSource?.MediaType);
        Assert.Null(task.ImportSource?.SourceUrl);
    }

    [Fact]
    public async Task CleanupExpiredAsync_ConcurrentReactivation_PreventsCleanup()
    {
        // Arrange
        var inactiveAt = DateTimeOffset.UnixEpoch;
        var graph = await SeedInactiveCourseAsync(inactiveAt);
        var cleanup = CreateCleanup(inactiveAt.AddDays(30));

        await using var reactivationContext = _fixture.CreateDbContext();
        await using var reactivationTransaction =
            await reactivationContext.Database.BeginTransactionAsync();
        await reactivationContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM course_subscriptions WHERE id = {graph.SubscriptionId} FOR UPDATE");
        var subscription = await reactivationContext.CourseSubscriptions
            .SingleAsync(candidate => candidate.Id == graph.SubscriptionId);
        subscription.BeginReactivation();
        await reactivationContext.SaveChangesAsync();

        // Act
        var cleanupTask = cleanup.CleanupExpiredAsync();
        await WaitUntilCourseIsLockedAsync(graph.CourseId);
        await reactivationTransaction.CommitAsync();
        var cleanedCourseCount = await cleanupTask;

        // Assert
        Assert.Equal(0, cleanedCourseCount);
        await using var context = _fixture.CreateDbContext();
        Assert.Equal(1, await context.ExternalCourses.CountAsync());
        Assert.Equal(1, await context.ScanRuns.CountAsync());
        Assert.Equal(1, await context.CourseSnapshots.CountAsync());
        var savedSubscription =
            await context.CourseSubscriptions.SingleAsync();
        Assert.Equal(
            CourseSubscriptionState.Pending,
            savedSubscription.State);
    }

    private async Task WaitUntilCourseIsLockedAsync(Guid courseId)
    {
        for (var attempt = 0; attempt < 1000; attempt++)
        {
            await using var context = _fixture.CreateDbContext();
            await using var transaction =
                await context.Database.BeginTransactionAsync();
            try
            {
                await context.Database.ExecuteSqlInterpolatedAsync(
                    $"SELECT 1 FROM external_courses WHERE id = {courseId} FOR UPDATE NOWAIT");
                await transaction.RollbackAsync();
            }
            catch (PostgresException exception)
                when (exception.SqlState ==
                    PostgresErrorCodes.LockNotAvailable)
            {
                return;
            }

            await Task.Yield();
        }

        throw new TimeoutException(
            "Cleanup did not acquire the External Course lock.");
    }

    private ExternalCourseCleanup CreateCleanup(DateTimeOffset now)
    {
        return new ExternalCourseCleanup(
            new TestDbContextFactory(_fixture),
            new ExternalCourseCleanupOptions(
                TimeSpan.FromDays(30),
                TimeSpan.FromHours(1)),
            new FixedTimeProvider(now));
    }

    private async Task<SeededCourse> SeedInactiveCourseAsync(
        DateTimeOffset inactiveAt,
        bool withPersonalReference = false)
    {
        await using var context = _fixture.CreateDbContext();
        var course = new ExternalCourse(
            new ExternalCourseIdentity(
                "mock-moodle",
                "https://moodle.example.test",
                "course-17"),
            "Software Engineering",
            inactiveAt.AddDays(-1));
        course.Activate();
        course.Deactivate(inactiveAt);

        var content = new ExternalLearningContent(
            course.Id,
            new ExternalContentKey("file-17"),
            ExternalLearningContentType.File,
            "Exercise sheet",
            inactiveAt.AddDays(-1),
            null,
            "application/pdf",
            "/mod/resource/17");
        var scanRun = new ScanRun(
            course.Id,
            inactiveAt.AddMinutes(-2),
            inactiveAt.AddMinutes(3));
        scanRun.Succeed(
            new ScanRunCounts(1, 0, 0, 0),
            inactiveAt.AddMinutes(-1));
        var snapshot = new CourseSnapshot(
            course.Id,
            scanRun.Id,
            inactiveAt.AddMinutes(-1));
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

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "cleanup@example.test",
            NormalizedUserName = "CLEANUP@EXAMPLE.TEST",
            Email = "cleanup@example.test",
            NormalizedEmail = "CLEANUP@EXAMPLE.TEST",
            SecurityStamp = Guid.NewGuid().ToString()
        };
        var module = new Domain.Modules.StudyModule(
            user.Id,
            "Software Engineering");
        var subscription = new CourseSubscription(
            module.Id,
            user.Id,
            course.Id,
            inactiveAt.AddDays(-1));
        subscription.Activate(inactiveAt.AddHours(-1));
        subscription.End(inactiveAt);

        context.Users.Add(user);
        context.Modules.Add(module);
        context.ExternalCourses.Add(course);
        context.CourseSubscriptions.Add(subscription);
        context.ExternalLearningContents.Add(content);
        context.ScanRuns.Add(scanRun);
        context.CourseSnapshots.Add(snapshot);
        context.CourseSnapshotItems.Add(snapshotItem);

        if (withPersonalReference)
        {
            var task = new Domain.Tasks.StudyTask(
                module.Id,
                content.Title,
                content.DueDate);
            var importState = new SubscriptionContentState(
                subscription.Id,
                course.Id,
                content.Id,
                task.Id,
                content.Signature,
                inactiveAt.AddMinutes(-1));
            context.Tasks.Add(task);
            context.SubscriptionContentStates.Add(importState);
        }

        await context.SaveChangesAsync();
        return new SeededCourse(
            course.Id,
            subscription.Id,
            content.Id,
            user.Id,
            module.Id);
    }

    private sealed class TestDbContextFactory(PostgreSqlFixture fixture)
        : IDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext()
        {
            return fixture.CreateDbContext();
        }

        public Task<ApplicationDbContext> CreateDbContextAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CreateDbContext());
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset now)
        : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return now;
        }
    }

    private sealed record SeededCourse(
        Guid CourseId,
        Guid SubscriptionId,
        Guid ContentId,
        Guid OwnerId,
        Guid ModuleId);
}
