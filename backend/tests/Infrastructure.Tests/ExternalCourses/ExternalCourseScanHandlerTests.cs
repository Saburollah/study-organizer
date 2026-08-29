using Microsoft.EntityFrameworkCore;
using StudyOrganizer.Application.ExternalCourses;
using StudyOrganizer.Domain.ExternalCourses;

namespace StudyOrganizer.Infrastructure.Tests.ExternalCourses;

public sealed class ExternalCourseScanHandlerTests
{
    [Fact]
    public async Task ScanAsync_ThreeSubscribers_FetchesOnceAndCreatesThreeTasks()
    {
        await using var setup = await ExternalCourseScenario.CreateAsync(
            subscriberCount: 3);
        setup.Provider.SetSnapshot(ExternalCourseSnapshots.Initial);

        var result = await setup.Handler.ScanAsync(
            setup.OwnerIds[0],
            setup.SubscriptionIds[0]);

        Assert.Equal(CourseScanOutcome.Succeeded, result.Outcome);
        Assert.Equal(1, setup.Provider.FetchCount);
        Assert.Equal(2, await setup.Database.Context.ExternalContents.CountAsync());
        Assert.Equal(3, await setup.Database.Context.Tasks.CountAsync());
        Assert.Equal(3, await setup.Database.Context.ExternalTaskLinks.CountAsync());
        Assert.Equal(1, result.Summary!.NewTaskEligibleCount);
        Assert.Equal(1, result.Summary.ReviewRequiredCount);
    }

    [Fact]
    public async Task ScanAsync_SameSnapshotTwice_DoesNotCreateDuplicates()
    {
        await using var setup = await ExternalCourseScenario.CreateAsync(
            subscriberCount: 1);
        setup.Provider.SetSnapshot(ExternalCourseSnapshots.Initial);

        await setup.Handler.ScanAsync(setup.OwnerIds[0], setup.SubscriptionIds[0]);
        await setup.Handler.ScanAsync(setup.OwnerIds[0], setup.SubscriptionIds[0]);

        Assert.Equal(2, setup.Provider.FetchCount);
        Assert.Single(setup.Database.Context.Tasks);
        Assert.Single(setup.Database.Context.ExternalTaskLinks);
    }

    [Fact]
    public async Task ScanAsync_InitialSnapshot_ClassifiesAndMapsSourceFields()
    {
        await using var setup = await ExternalCourseScenario.CreateAsync(
            subscriberCount: 1);
        setup.Provider.SetSnapshot(ExternalCourseSnapshots.Initial);

        var result = await setup.Handler.ScanAsync(
            setup.OwnerIds[0],
            setup.SubscriptionIds[0]);

        var exercise = await setup.Database.Context.ExternalContents.SingleAsync(
            content => content.ProviderContentId == "exercise-1");
        var announcement = await setup.Database.Context.ExternalContents.SingleAsync(
            content => content.ProviderContentId == "announcement-1");
        var task = await setup.Database.Context.Tasks.SingleAsync();
        Assert.Equal(ExternalContentProcessingState.TaskEligible, exercise.ProcessingState);
        Assert.Equal(ExternalContentReviewReason.None, exercise.ReviewReason);
        Assert.Equal(ExternalContentProcessingState.ReviewRequired, announcement.ProcessingState);
        Assert.Equal(ExternalContentReviewReason.NotAnAssignment, announcement.ReviewReason);
        Assert.Equal("Exercise 1", task.Title);
        Assert.Null(task.Description);
        Assert.Equal(new DateTimeOffset(2026, 9, 15, 12, 0, 0, TimeSpan.Zero), task.DueDate);
        Assert.Equal(setup.ModuleIds[0], task.ModuleId);
        Assert.Equal(2, result.Summary!.NewContentCount);
        Assert.Equal(0, result.Summary.ChangedContentCount);
        Assert.Equal(0, result.Summary.NotVisibleCount);
        Assert.Equal(1, result.Summary.NewTaskEligibleCount);
        Assert.Single(await setup.Database.Context.ExternalTaskLinks.ToListAsync());
    }

    [Fact]
    public async Task ScanAsync_AssignmentWithoutStructuredDeadline_RequiresReviewWithoutTask()
    {
        await using var setup = await ExternalCourseScenario.CreateAsync(
            subscriberCount: 1);
        setup.Provider.SetSnapshot(new CourseSnapshot(
            "mock-moodle",
            "software-engineering-2026",
            true,
            [
                ExternalCourseSnapshots.Initial.Contents[0] with
                {
                    StructuredDueDateUtc = null
                }
            ]));

        var result = await setup.Handler.ScanAsync(
            setup.OwnerIds[0],
            setup.SubscriptionIds[0]);

        var content = await setup.Database.Context.ExternalContents.SingleAsync();
        Assert.Equal(ExternalContentProcessingState.ReviewRequired, content.ProcessingState);
        Assert.Equal(
            ExternalContentReviewReason.MissingStructuredDeadline,
            content.ReviewReason);
        Assert.Equal(1, result.Summary!.ReviewRequiredCount);
        Assert.Equal(0, result.Summary.NewTaskEligibleCount);
        Assert.Empty(await setup.Database.Context.Tasks.ToListAsync());
        Assert.Empty(await setup.Database.Context.ExternalTaskLinks.ToListAsync());
    }

    [Theory]
    [MemberData(nameof(InvalidSnapshots))]
    public async Task ScanAsync_InvalidSnapshot_ReturnsInvalidWithoutMaterializing(
        CourseSnapshot snapshot)
    {
        await using var setup = await ExternalCourseScenario.CreateAsync(
            subscriberCount: 1);
        setup.Provider.SetSnapshot(snapshot);

        var result = await setup.Handler.ScanAsync(
            setup.OwnerIds[0],
            setup.SubscriptionIds[0]);

        Assert.Equal(CourseScanOutcome.InvalidSnapshot, result.Outcome);
        Assert.Null(result.Summary);
        Assert.Empty(await setup.Database.Context.ExternalContents.ToListAsync());
        Assert.Empty(await setup.Database.Context.Tasks.ToListAsync());
        Assert.Empty(await setup.Database.Context.ExternalTaskLinks.ToListAsync());
    }

    [Fact]
    public async Task ScanAsync_ContentIdsCollideAfterCanonicalization_RejectsBeforeMutation()
    {
        await using var setup = await ExternalCourseScenario.CreateAsync(
            subscriberCount: 1);
        setup.Provider.SetSnapshot(ExternalCourseSnapshots.Initial with
        {
            Contents =
            [
                ExternalCourseSnapshots.Initial.Contents[0],
                ExternalCourseSnapshots.Initial.Contents[0] with
                {
                    ProviderContentId = " exercise-1 ",
                    Title = "Canonical collision"
                }
            ]
        });

        var result = await setup.Handler.ScanAsync(
            setup.OwnerIds[0],
            setup.SubscriptionIds[0]);

        Assert.Equal(CourseScanOutcome.InvalidSnapshot, result.Outcome);
        Assert.Empty(await setup.Database.Context.ExternalContents.ToListAsync());
        Assert.Empty(await setup.Database.Context.Tasks.ToListAsync());
        Assert.Empty(await setup.Database.Context.ExternalTaskLinks.ToListAsync());
    }

    [Fact]
    public async Task ScanAsync_KnownContentIdWithWhitespace_RemainsSameIdentityAndIdempotent()
    {
        await using var setup = await ExternalCourseScenario.CreateAsync(
            subscriberCount: 1);
        setup.Provider.SetSnapshot(ExternalCourseSnapshots.Initial);
        await setup.Handler.ScanAsync(setup.OwnerIds[0], setup.SubscriptionIds[0]);
        var originalContentId = await setup.Database.Context.ExternalContents
            .Where(content => content.ProviderContentId == "exercise-1")
            .Select(content => content.Id)
            .SingleAsync();
        setup.Provider.SetSnapshot(ExternalCourseSnapshots.Initial with
        {
            Contents =
            [
                ExternalCourseSnapshots.Initial.Contents[0] with
                {
                    ProviderContentId = " exercise-1 "
                },
                ExternalCourseSnapshots.Initial.Contents[1]
            ]
        });

        var result = await setup.Handler.ScanAsync(
            setup.OwnerIds[0],
            setup.SubscriptionIds[0]);

        var exercise = await setup.Database.Context.ExternalContents
            .SingleAsync(content => content.ProviderContentId == "exercise-1");
        Assert.Equal(CourseScanOutcome.Succeeded, result.Outcome);
        Assert.Equal(originalContentId, exercise.Id);
        Assert.Equal(0, result.Summary!.NewContentCount);
        Assert.Equal(0, result.Summary.ChangedContentCount);
        Assert.Equal(2, setup.Provider.FetchCount);
        Assert.Equal(2, await setup.Database.Context.ExternalContents.CountAsync());
        Assert.Single(await setup.Database.Context.Tasks.ToListAsync());
        Assert.Single(await setup.Database.Context.ExternalTaskLinks.ToListAsync());
    }

    [Fact]
    public async Task ScanAsync_BlockedFetch_PersistsLeaseAndInProgressRunBeforeFetchCompletes()
    {
        await using var setup = await ExternalCourseScenario.CreateAsync(
            subscriberCount: 1);
        setup.Provider.SetSnapshot(ExternalCourseSnapshots.Initial);
        setup.Provider.BlockNextFetch();

        var scan = setup.Handler.ScanAsync(
            setup.OwnerIds[0],
            setup.SubscriptionIds[0]);

        try
        {
            Assert.Equal(1, setup.Provider.FetchCount);
            var course = await setup.Database.Context.ExternalCourses.SingleAsync();
            var run = await setup.Database.Context.ScanRuns.SingleAsync();
            Assert.NotNull(course.ActiveScanRunId);
            Assert.Equal(course.ActiveScanRunId, run.Id);
            Assert.Equal(ScanRunStatus.InProgress, run.Status);
        }
        finally
        {
            setup.Provider.ReleaseBlockedFetch();
        }

        var result = await scan;
        setup.Database.Context.ChangeTracker.Clear();
        var completedCourse = await setup.Database.Context.ExternalCourses.SingleAsync();
        var completedRun = await setup.Database.Context.ScanRuns.SingleAsync();
        Assert.Equal(CourseScanOutcome.Succeeded, result.Outcome);
        Assert.Null(completedCourse.ActiveScanRunId);
        Assert.Equal(setup.Database.Now, completedCourse.LastSuccessfulScanAtUtc);
        Assert.Equal(ScanRunStatus.Succeeded, completedRun.Status);
        Assert.Equal(setup.Database.Now, completedRun.FinishedAtUtc);
    }

    [Fact]
    public async Task ScanAsync_UnknownSubscription_ReturnsNotFoundWithoutFetch()
    {
        await using var setup = await ExternalCourseScenario.CreateAsync(
            subscriberCount: 1);

        var result = await setup.Handler.ScanAsync(setup.OwnerIds[0], Guid.NewGuid());

        Assert.Equal(CourseScanOutcome.NotFound, result.Outcome);
        Assert.Equal(0, setup.Provider.FetchCount);
        Assert.Empty(await setup.Database.Context.ScanRuns.ToListAsync());
    }

    public static TheoryData<CourseSnapshot> InvalidSnapshots => new()
    {
        ExternalCourseSnapshots.WrongCourse,
        ExternalCourseSnapshots.DuplicateContentIds,
        new CourseSnapshot(
            "wrong-provider",
            "software-engineering-2026",
            true,
            ExternalCourseSnapshots.Initial.Contents),
        ExternalCourseSnapshots.Initial with { IsComplete = false },
        ExternalCourseSnapshots.Initial with
        {
            Contents =
            [
                ExternalCourseSnapshots.Initial.Contents[0] with { Title = "   " }
            ]
        },
        ExternalCourseSnapshots.Initial with
        {
            Contents =
            [
                ExternalCourseSnapshots.Initial.Contents[0] with
                {
                    SourceUri = new Uri("/relative/exercise-1", UriKind.Relative)
                }
            ]
        },
        ExternalCourseSnapshots.Initial with
        {
            Contents =
            [
                ExternalCourseSnapshots.Initial.Contents[0] with
                {
                    SourceUri = new Uri("ftp://mock-moodle.local/content/exercise-1")
                }
            ]
        }
    };
}
