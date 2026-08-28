using StudyOrganizer.Application.ExternalCourses;
using StudyOrganizer.Domain.ExternalCourses;
using StudyOrganizer.Domain.Modules;
using StudyOrganizer.Domain.Tasks;
using StudyOrganizer.Infrastructure.ExternalCourses;

namespace StudyOrganizer.Infrastructure.Tests.ExternalCourses;

public sealed class ExternalCourseQueryHandlerTests
{
    [Fact]
    public async Task GetByOwnerAsync_ReturnsOnlyOwnerSubscriptionsWithDerivedScanStatus()
    {
        await using var database = await ExternalCourseTestDatabase.CreateAsync();
        var ownerId = await database.CreateUserAsync("owner@example.com");
        var otherOwnerId = await database.CreateUserAsync("other@example.com");
        var neverScanned = CreateSubscription(database, ownerId, "Never scanned");
        var completed = CreateSubscription(database, ownerId, "Completed scan");
        var active = CreateSubscription(database, ownerId, "Active scan");
        _ = CreateSubscription(database, otherOwnerId, "Foreign course");

        var failedRun = new ScanRun(completed.Course.Id, ownerId, database.Now);
        failedRun.Fail("external_timeout", database.Now.AddMinutes(1));
        var succeededRun = new ScanRun(
            completed.Course.Id,
            ownerId,
            database.Now.AddMinutes(2));
        succeededRun.Succeed(database.Now.AddMinutes(3));
        completed.Course.MarkScanStarted(succeededRun.Id);
        completed.Course.MarkScanSucceeded(succeededRun.Id, database.Now.AddMinutes(3));
        var activeRun = new ScanRun(active.Course.Id, ownerId, database.Now.AddMinutes(4));
        active.Course.MarkScanStarted(activeRun.Id);
        database.Context.ScanRuns.AddRange(failedRun, succeededRun, activeRun);
        await database.Context.SaveChangesAsync();
        var handler = new ExternalCourseQueryHandler(database.Context);

        var results = await handler.GetByOwnerAsync(ownerId);

        Assert.Equal(3, results.Count);
        Assert.DoesNotContain(results, result => result.CourseName == "Foreign course");
        Assert.Equal(
            "NeverScanned",
            Assert.Single(results, result => result.Id == neverScanned.Subscription.Id)
                .LastScanStatus);
        var completedResult = Assert.Single(
            results,
            result => result.Id == completed.Subscription.Id);
        Assert.Equal("Succeeded", completedResult.LastScanStatus);
        Assert.Equal(database.Now.AddMinutes(3), completedResult.LastSuccessfulScanAtUtc);
        Assert.Equal(
            "InProgress",
            Assert.Single(results, result => result.Id == active.Subscription.Id)
                .LastScanStatus);
    }

    [Fact]
    public async Task GetContentsAsync_ForeignOrMissingSubscription_ReturnsNull()
    {
        await using var database = await ExternalCourseTestDatabase.CreateAsync();
        var ownerId = await database.CreateUserAsync("owner@example.com");
        var otherOwnerId = await database.CreateUserAsync("other@example.com");
        var subscription = CreateSubscription(database, otherOwnerId, "Foreign course");
        await database.Context.SaveChangesAsync();
        var handler = new ExternalCourseQueryHandler(database.Context);

        var foreign = await handler.GetContentsAsync(ownerId, subscription.Subscription.Id);
        var missing = await handler.GetContentsAsync(ownerId, Guid.NewGuid());

        Assert.Null(foreign);
        Assert.Null(missing);
    }

    [Fact]
    public async Task GetContentsAsync_ProjectsVisibilityReviewAndSubscriptionScopedTaskLink()
    {
        await using var database = await ExternalCourseTestDatabase.CreateAsync();
        var ownerId = await database.CreateUserAsync("owner@example.com");
        var otherOwnerId = await database.CreateUserAsync("other@example.com");
        var owned = CreateSubscription(database, ownerId, "Shared course");
        var otherModule = new StudyModule(otherOwnerId, "Shared course");
        var otherSubscription = new CourseSubscription(
            otherOwnerId,
            owned.Course.Id,
            otherModule.Id,
            database.Now);
        var taskCreated = CreateContent(
            owned.Course.Id,
            "exercise-1",
            ExternalContentKind.Assignment,
            ExternalContentProcessingState.TaskEligible,
            ExternalContentReviewReason.None,
            database.Now.AddDays(1));
        var reviewRequired = CreateContent(
            owned.Course.Id,
            "announcement-1",
            ExternalContentKind.Announcement,
            ExternalContentProcessingState.ReviewRequired,
            ExternalContentReviewReason.NotAnAssignment,
            null);
        var notVisible = CreateContent(
            owned.Course.Id,
            "exercise-hidden",
            ExternalContentKind.Assignment,
            ExternalContentProcessingState.TaskEligible,
            ExternalContentReviewReason.None,
            database.Now.AddDays(2));
        notVisible.MarkNotVisible();
        var ownedTask = new StudyTask(
            owned.Module.Id,
            "Exercise 1",
            database.Now.AddDays(1));
        var hiddenTask = new StudyTask(
            owned.Module.Id,
            "Hidden exercise",
            database.Now.AddDays(2));
        database.Context.AddRange(
            otherModule,
            otherSubscription,
            taskCreated,
            reviewRequired,
            notVisible,
            ownedTask,
            hiddenTask);
        database.Context.ExternalTaskLinks.AddRange(
            new ExternalTaskLink(
                owned.Subscription.Id,
                taskCreated.Id,
                ownedTask.Id,
                database.Now),
            new ExternalTaskLink(
                owned.Subscription.Id,
                notVisible.Id,
                hiddenTask.Id,
                database.Now));
        await database.Context.SaveChangesAsync();
        var handler = new ExternalCourseQueryHandler(database.Context);

        var ownedResults = await handler.GetContentsAsync(
            ownerId,
            owned.Subscription.Id);
        var otherResults = await handler.GetContentsAsync(
            otherOwnerId,
            otherSubscription.Id);

        Assert.NotNull(ownedResults);
        Assert.Equal(3, ownedResults.Count);
        var taskResult = Assert.Single(
            ownedResults,
            result => result.ProviderContentId == "exercise-1");
        Assert.Equal(ExternalContentDisplayStatus.TaskCreated, taskResult.Status);
        Assert.Equal(ownedTask.Id, taskResult.TaskId);
        Assert.Null(taskResult.ReviewReason);
        var reviewResult = Assert.Single(
            ownedResults,
            result => result.ProviderContentId == "announcement-1");
        Assert.Equal(ExternalContentDisplayStatus.ReviewRequired, reviewResult.Status);
        Assert.Equal("NotAnAssignment", reviewResult.ReviewReason);
        Assert.Null(reviewResult.TaskId);
        var hiddenResult = Assert.Single(
            ownedResults,
            result => result.ProviderContentId == "exercise-hidden");
        Assert.Equal(ExternalContentDisplayStatus.NotVisible, hiddenResult.Status);
        Assert.Equal(hiddenTask.Id, hiddenResult.TaskId);
        Assert.NotNull(otherResults);
        Assert.All(
            otherResults,
            result =>
            {
                Assert.NotEqual(ExternalContentDisplayStatus.TaskCreated, result.Status);
                Assert.Null(result.TaskId);
            });
    }

    private static SubscriptionFixture CreateSubscription(
        ExternalCourseTestDatabase database,
        Guid ownerId,
        string courseName)
    {
        var course = new ExternalCourse(
            "mock-moodle",
            Guid.NewGuid().ToString("N"),
            courseName,
            database.Now);
        var module = new StudyModule(ownerId, courseName);
        var subscription = new CourseSubscription(
            ownerId,
            course.Id,
            module.Id,
            database.Now);
        database.Context.AddRange(course, module, subscription);
        return new SubscriptionFixture(course, module, subscription);
    }

    private static ExternalContent CreateContent(
        Guid courseId,
        string providerContentId,
        ExternalContentKind kind,
        ExternalContentProcessingState processingState,
        ExternalContentReviewReason reviewReason,
        DateTimeOffset? dueDate)
    {
        return ExternalContent.Create(
            courseId,
            providerContentId,
            kind,
            providerContentId,
            $"Description for {providerContentId}",
            $"https://mock-moodle.local/content/{providerContentId}",
            dueDate,
            processingState,
            reviewReason,
            new DateTimeOffset(2026, 8, 28, 8, 0, 0, TimeSpan.Zero));
    }

    private sealed record SubscriptionFixture(
        ExternalCourse Course,
        StudyModule Module,
        CourseSubscription Subscription);
}
