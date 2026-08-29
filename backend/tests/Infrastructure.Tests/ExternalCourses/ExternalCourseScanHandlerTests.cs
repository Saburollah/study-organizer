using Microsoft.EntityFrameworkCore;
using StudyOrganizer.Application.ExternalCourses;
using StudyOrganizer.Domain.ExternalCourses;
using StudyOrganizer.Domain.Tasks;
using StudyOrganizer.Infrastructure.ExternalCourses;

namespace StudyOrganizer.Infrastructure.Tests.ExternalCourses;

public sealed class ExternalCourseScanHandlerTests
{
    [Fact]
    public async Task ScanAsync_ChangedContent_SynchronizesOpenTaskAndCreatesNewExerciseTask()
    {
        await using var setup = await ExternalCourseScenario.CreateScannedAsync(
            subscriberCount: 1);
        var originalTaskId = setup.TaskIds[0];
        setup.Provider.SetSnapshot(ExternalCourseSnapshots.Changed);

        var result = await setup.Handler.ScanAsync(
            setup.OwnerIds[0],
            setup.SubscriptionIds[0]);

        var tasks = await setup.TasksForAsync(setup.OwnerIds[0]);
        var updatedExerciseOne = Assert.Single(tasks, task => task.Id == originalTaskId);
        Assert.Equal("Exercise 1 revised", updatedExerciseOne.Title);
        Assert.Equal(
            new DateTimeOffset(2026, 9, 17, 12, 0, 0, TimeSpan.Zero),
            updatedExerciseOne.DueDate);
        Assert.Equal(StudyTaskStatus.Open, updatedExerciseOne.Status);
        Assert.Equal(2, tasks.Count);
        Assert.Contains(tasks, task => task.Title == "Exercise 2");
        Assert.Equal(1, result.Summary!.ChangedContentCount);
        Assert.Equal(1, result.Summary.NewTaskEligibleCount);
    }

    [Fact]
    public async Task ScanAsync_ChangedContent_DoesNotOverwriteCompletedLinkedTask()
    {
        await using var setup = await ExternalCourseScenario.CreateScannedAsync(
            subscriberCount: 1);
        var originalTask = await setup.ReloadTaskAsync(setup.TaskIds[0]);
        originalTask.Complete();
        await setup.Database.Context.SaveChangesAsync();
        var originalTitle = originalTask.Title;
        var originalDueDate = originalTask.DueDate;
        setup.Provider.SetSnapshot(ExternalCourseSnapshots.Changed);

        await setup.Handler.ScanAsync(setup.OwnerIds[0], setup.SubscriptionIds[0]);

        var completedTask = await setup.ReloadTaskAsync(originalTask.Id);
        Assert.Equal(StudyTaskStatus.Completed, completedTask.Status);
        Assert.Equal(originalTitle, completedTask.Title);
        Assert.Equal(originalDueDate, completedTask.DueDate);
    }

    [Fact]
    public async Task ScanAsync_TaskEligibleContentLosesDeadline_PreservesLinkedTaskAndRequiresReview()
    {
        await using var setup = await ExternalCourseScenario.CreateScannedAsync(
            subscriberCount: 1);
        var taskId = setup.TaskIds[0];
        var originalTask = await setup.ReloadTaskAsync(taskId);
        var originalTitle = originalTask.Title;
        var originalDueDate = originalTask.DueDate;
        setup.Provider.SetSnapshot(ExternalCourseSnapshots.Initial with
        {
            Contents =
            [
                ExternalCourseSnapshots.Initial.Contents[0] with
                {
                    Title = "Exercise 1 without deadline",
                    StructuredDueDateUtc = null
                },
                ExternalCourseSnapshots.Initial.Contents[1]
            ]
        });

        await setup.Handler.ScanAsync(setup.OwnerIds[0], setup.SubscriptionIds[0]);

        var content = await setup.Database.Context.ExternalContents.SingleAsync(
            item => item.ProviderContentId == "exercise-1");
        var linkedTask = await setup.ReloadTaskAsync(taskId);
        Assert.Equal(ExternalContentProcessingState.ReviewRequired, content.ProcessingState);
        Assert.Equal(
            ExternalContentReviewReason.MissingStructuredDeadline,
            content.ReviewReason);
        Assert.Equal(originalTitle, linkedTask.Title);
        Assert.Equal(originalDueDate, linkedTask.DueDate);
        Assert.Single(await setup.Database.Context.ExternalTaskLinks.ToListAsync());
    }

    [Fact]
    public async Task ScanAsync_CompleteSnapshotOmitsContent_MarksItNotVisibleWithoutDeletingTaskOrLink()
    {
        await using var setup = await ExternalCourseScenario.CreateScannedAsync(
            subscriberCount: 1);
        setup.Provider.SetSnapshot(ExternalCourseSnapshots.WithoutExerciseOne);

        var result = await setup.Handler.ScanAsync(
            setup.OwnerIds[0],
            setup.SubscriptionIds[0]);

        var missing = await setup.Database.Context.ExternalContents.SingleAsync(
            content => content.ProviderContentId == "exercise-1");
        Assert.Equal(ExternalContentVisibility.NotVisible, missing.Visibility);
        Assert.Single(await setup.Database.Context.Tasks.ToListAsync());
        Assert.Single(await setup.Database.Context.ExternalTaskLinks.ToListAsync());
        Assert.Equal(1, result.Summary!.NotVisibleCount);
    }

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

    [Theory]
    [InlineData(ExternalCourseProviderError.Timeout, "external_timeout")]
    [InlineData(ExternalCourseProviderError.AuthenticationRequired, "external_auth_required")]
    public async Task ScanAsync_ProviderFailure_PreservesStateAndStoresOnlySafeAuditCode(
        ExternalCourseProviderError providerError,
        string expectedErrorCode)
    {
        await using var setup = await ExternalCourseScenario.CreateScannedAsync(
            subscriberCount: 1);
        var originalContent = await setup.Database.Context.ExternalContents
            .AsNoTracking()
            .SingleAsync(content => content.ProviderContentId == "exercise-1");
        var originalTask = await setup.Database.Context.Tasks.AsNoTracking().SingleAsync();
        setup.Provider.SetFailure(providerError);

        var result = await setup.Handler.ScanAsync(
            setup.OwnerIds[0],
            setup.SubscriptionIds[0]);

        setup.Database.Context.ChangeTracker.Clear();
        var currentContent = await setup.Database.Context.ExternalContents
            .SingleAsync(content => content.Id == originalContent.Id);
        var currentTask = await setup.Database.Context.Tasks
            .SingleAsync(task => task.Id == originalTask.Id);
        var failedRun = await setup.Database.Context.ScanRuns
            .AsNoTracking()
            .SingleAsync(run => run.Status == ScanRunStatus.Failed);
        var course = await setup.Database.Context.ExternalCourses.SingleAsync();
        Assert.Equal(CourseScanOutcome.ExternalFailure, result.Outcome);
        Assert.Equal(expectedErrorCode, result.ErrorCode);
        Assert.Equal(expectedErrorCode, failedRun.ErrorCode);
        Assert.Equal(ScanRunStatus.Failed, failedRun.Status);
        Assert.Null(course.ActiveScanRunId);
        Assert.Equal(originalContent.Title, currentContent.Title);
        Assert.Equal(originalContent.StructuredDueDateUtc, currentContent.StructuredDueDateUtc);
        Assert.Equal(originalTask.Title, currentTask.Title);
        Assert.Equal(originalTask.DueDate, currentTask.DueDate);
        Assert.DoesNotContain("provider", failedRun.ErrorCode, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [MemberData(nameof(UnsafeInvalidSnapshots))]
    public async Task ScanAsync_InvalidIdentityOrDuplicates_PreservesStateAndStoresNoRawDetail(
        CourseSnapshot invalidSnapshot,
        string forbiddenDetail)
    {
        await using var setup = await ExternalCourseScenario.CreateScannedAsync(
            subscriberCount: 1);
        var contentsBefore = await setup.Database.Context.ExternalContents
            .AsNoTracking()
            .OrderBy(content => content.ProviderContentId)
            .Select(content => new { content.Id, content.Title, content.SourceUrl })
            .ToArrayAsync();
        var taskBefore = await setup.Database.Context.Tasks.AsNoTracking().SingleAsync();
        setup.Provider.SetSnapshot(invalidSnapshot);

        var result = await setup.Handler.ScanAsync(
            setup.OwnerIds[0],
            setup.SubscriptionIds[0]);

        setup.Database.Context.ChangeTracker.Clear();
        var contentsAfter = await setup.Database.Context.ExternalContents
            .AsNoTracking()
            .OrderBy(content => content.ProviderContentId)
            .Select(content => new { content.Id, content.Title, content.SourceUrl })
            .ToArrayAsync();
        var taskAfter = await setup.Database.Context.Tasks.AsNoTracking().SingleAsync();
        var failedRun = await setup.Database.Context.ScanRuns
            .AsNoTracking()
            .SingleAsync(run => run.Status == ScanRunStatus.Failed);
        var course = await setup.Database.Context.ExternalCourses.SingleAsync();
        Assert.Equal(CourseScanOutcome.InvalidSnapshot, result.Outcome);
        Assert.Equal("invalid_external_response", result.ErrorCode);
        Assert.Equal(contentsBefore, contentsAfter);
        Assert.Equal(taskBefore.Id, taskAfter.Id);
        Assert.Equal(taskBefore.Title, taskAfter.Title);
        Assert.Equal(taskBefore.DueDate, taskAfter.DueDate);
        Assert.Equal("invalid_external_response", failedRun.ErrorCode);
        Assert.DoesNotContain(forbiddenDetail, failedRun.ErrorCode, StringComparison.Ordinal);
        Assert.DoesNotContain("raw-query", failedRun.ErrorCode, StringComparison.Ordinal);
        Assert.DoesNotContain("raw-snapshot", failedRun.ErrorCode, StringComparison.Ordinal);
        Assert.Null(course.ActiveScanRunId);
    }

    [Fact]
    public async Task ScanAsync_CancelledFetch_RecordsCancellationClearsLeaseAndRethrows()
    {
        await using var setup = await ExternalCourseScenario.CreateAsync(
            subscriberCount: 1);
        setup.Provider.SetSnapshot(ExternalCourseSnapshots.Initial);
        setup.Provider.BlockNextFetch();
        using var cancellation = new CancellationTokenSource();
        var scan = setup.Handler.ScanAsync(
            setup.OwnerIds[0],
            setup.SubscriptionIds[0],
            cancellation.Token);
        await setup.Provider.WaitForFetchAsync();

        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => scan);
        setup.Provider.ReleaseBlockedFetch();
        setup.Database.Context.ChangeTracker.Clear();
        var run = await setup.Database.Context.ScanRuns.SingleAsync();
        var course = await setup.Database.Context.ExternalCourses.SingleAsync();
        Assert.Equal(ScanRunStatus.Failed, run.Status);
        Assert.Equal("scan_cancelled", run.ErrorCode);
        Assert.Null(course.ActiveScanRunId);
        Assert.Empty(await setup.Database.Context.ExternalContents.ToListAsync());
    }

    [Fact]
    public async Task ScanAsync_UnexpectedProviderFailure_RecordsFailureClearsLeaseAndRethrows()
    {
        await using var setup = await ExternalCourseScenario.CreateAsync(
            subscriberCount: 1);
        setup.Provider.SetUnexpectedFailure(
            new InvalidOperationException("secret payload https://mock-moodle.local/?token=raw"));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            setup.Handler.ScanAsync(setup.OwnerIds[0], setup.SubscriptionIds[0]));

        setup.Database.Context.ChangeTracker.Clear();
        var run = await setup.Database.Context.ScanRuns.SingleAsync();
        var course = await setup.Database.Context.ExternalCourses.SingleAsync();
        Assert.Contains("secret payload", exception.Message, StringComparison.Ordinal);
        Assert.Equal("scan_failed", run.ErrorCode);
        Assert.DoesNotContain("secret", run.ErrorCode, StringComparison.Ordinal);
        Assert.Null(course.ActiveScanRunId);
    }

    [Fact]
    public async Task ScanAsync_PersistenceFailure_RollsBackContentRecordsFailureAndClearsLease()
    {
        await using var setup = await ExternalCourseScenario.CreateAsync(
            subscriberCount: 1);
        setup.Provider.SetSnapshot(ExternalCourseSnapshots.Initial);
        await setup.Database.Context.Database.ExecuteSqlRawAsync(
            """
            CREATE TRIGGER fail_external_content_insert
            BEFORE INSERT ON external_contents
            BEGIN
                SELECT RAISE(ABORT, 'forced content persistence failure');
            END;
            """);

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            setup.Handler.ScanAsync(setup.OwnerIds[0], setup.SubscriptionIds[0]));

        setup.Database.Context.ChangeTracker.Clear();
        var run = await setup.Database.Context.ScanRuns.SingleAsync();
        var course = await setup.Database.Context.ExternalCourses.SingleAsync();
        Assert.Equal(ScanRunStatus.Failed, run.Status);
        Assert.Equal("scan_failed", run.ErrorCode);
        Assert.Null(course.ActiveScanRunId);
        Assert.Empty(await setup.Database.Context.ExternalContents.ToListAsync());
        Assert.Empty(await setup.Database.Context.Tasks.ToListAsync());
        Assert.Empty(await setup.Database.Context.ExternalTaskLinks.ToListAsync());
    }

    [Fact]
    public async Task ScanAsync_ConcurrentScopes_FetchesOnceAndReturnsAlreadyRunning()
    {
        await using var setup = await ExternalCourseScenario.CreateAsync(
            subscriberCount: 1);
        setup.Provider.SetSnapshot(ExternalCourseSnapshots.Initial);
        setup.Provider.BlockNextFetch();
        await using var secondContext = setup.Database.CreateContext();
        var secondHandler = new ExternalCourseScanHandler(
            secondContext,
            [setup.Provider],
            setup.Database.TimeProvider);
        var firstScan = setup.Handler.ScanAsync(
            setup.OwnerIds[0],
            setup.SubscriptionIds[0]);
        await setup.Provider.WaitForFetchAsync();

        var secondResult = await secondHandler.ScanAsync(
            setup.OwnerIds[0],
            setup.SubscriptionIds[0]);
        setup.Provider.ReleaseBlockedFetch();
        var firstResult = await firstScan;

        Assert.Equal(1, setup.Provider.FetchCount);
        Assert.Equal(CourseScanOutcome.Succeeded, firstResult.Outcome);
        Assert.Equal(CourseScanOutcome.AlreadyRunning, secondResult.Outcome);
        setup.Database.Context.ChangeTracker.Clear();
        Assert.Null((await setup.Database.Context.ExternalCourses.SingleAsync()).ActiveScanRunId);
        Assert.Single(await setup.Database.Context.ScanRuns
            .Where(run => run.Status == ScanRunStatus.Succeeded)
            .ToListAsync());
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

    public static TheoryData<CourseSnapshot, string> UnsafeInvalidSnapshots => new()
    {
        {
            ExternalCourseSnapshots.WrongCourse with
            {
                Contents =
                [
                    ExternalCourseSnapshots.Initial.Contents[0] with
                    {
                        Title = "{\"payload\":\"raw-snapshot\"}",
                        SourceUri = new Uri(
                            "https://mock-moodle.local/content/exercise-1?token=raw-query")
                    }
                ]
            },
            "raw-query"
        },
        { ExternalCourseSnapshots.DuplicateContentIds, "Duplicate exercise" }
    };
}
