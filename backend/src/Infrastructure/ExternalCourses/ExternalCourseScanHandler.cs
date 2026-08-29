using Microsoft.EntityFrameworkCore;
using StudyOrganizer.Application.ExternalCourses;
using StudyOrganizer.Domain.ExternalCourses;
using StudyOrganizer.Domain.Tasks;
using StudyOrganizer.Infrastructure.Persistence;

namespace StudyOrganizer.Infrastructure.ExternalCourses;

public sealed class ExternalCourseScanHandler(
    ApplicationDbContext dbContext,
    IEnumerable<IExternalCourseProvider> providers,
    TimeProvider timeProvider)
    : IExternalCourseScanHandler
{
    private const string InvalidSnapshotErrorCode = "invalid_external_response";

    private readonly IReadOnlyList<IExternalCourseProvider> _providers =
        providers.ToList();

    public async Task<CourseScanResult> ScanAsync(
        Guid ownerId,
        Guid subscriptionId,
        CancellationToken cancellationToken = default)
    {
        var target = await (
                from subscription in dbContext.CourseSubscriptions.AsNoTracking()
                join course in dbContext.ExternalCourses.AsNoTracking()
                    on subscription.ExternalCourseId equals course.Id
                where subscription.Id == subscriptionId
                    && subscription.OwnerId == ownerId
                select new ScanTarget(
                    course.Id,
                    course.ProviderKey,
                    course.ExternalCourseId))
            .SingleOrDefaultAsync(cancellationToken);

        if (target is null)
        {
            return new CourseScanResult(CourseScanOutcome.NotFound, null, null);
        }

        var provider = _providers.SingleOrDefault(item =>
            string.Equals(item.ProviderKey, target.ProviderKey, StringComparison.Ordinal));
        if (provider is null)
        {
            return new CourseScanResult(
                CourseScanOutcome.ExternalFailure,
                null,
                InvalidSnapshotErrorCode);
        }

        var startedAtUtc = timeProvider.GetUtcNow();
        var scanRun = new ScanRun(target.CourseId, ownerId, startedAtUtc);
        var acquired = await dbContext.ExternalCourses
            .Where(course => course.Id == target.CourseId
                && course.ActiveScanRunId == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    course => course.ActiveScanRunId,
                    scanRun.Id),
                cancellationToken);

        if (acquired == 0)
        {
            return new CourseScanResult(
                CourseScanOutcome.AlreadyRunning,
                null,
                null);
        }

        dbContext.ChangeTracker.Clear();
        dbContext.ScanRuns.Add(scanRun);
        await dbContext.SaveChangesAsync(cancellationToken);

        var snapshot = CanonicalizeSnapshot(
            await provider.FetchSnapshotAsync(
                target.ExternalCourseId,
                cancellationToken));

        if (!IsValidSnapshot(snapshot, target))
        {
            return new CourseScanResult(
                CourseScanOutcome.InvalidSnapshot,
                null,
                InvalidSnapshotErrorCode);
        }

        return await PersistSuccessfulScanAsync(
            target,
            snapshot,
            scanRun,
            cancellationToken);
    }

    private async Task<CourseScanResult> PersistSuccessfulScanAsync(
        ScanTarget target,
        CourseSnapshot snapshot,
        ScanRun scanRun,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database
            .BeginTransactionAsync(cancellationToken);

        try
        {
            var course = await dbContext.ExternalCourses.SingleAsync(
                item => item.Id == target.CourseId,
                cancellationToken);
            var existingContents = await dbContext.ExternalContents
                .Where(content => content.ExternalCourseId == target.CourseId)
                .ToListAsync(cancellationToken);
            var existingById = existingContents.ToDictionary(content => content.Id);
            var existingStates = existingContents
                .Select(content => new ExistingContentState(
                    content.Id,
                    content.ProviderContentId,
                    content.Kind,
                    content.Title,
                    content.Description,
                    content.SourceUrl,
                    content.StructuredDueDateUtc))
                .ToArray();
            var diff = CourseSnapshotDiffer.Compare(existingStates, snapshot);
            var subscriptions = await dbContext.CourseSubscriptions
                .Where(subscription => subscription.ExternalCourseId == target.CourseId)
                .ToListAsync(cancellationToken);
            var existingLinks = await dbContext.ExternalTaskLinks
                .Where(link => subscriptions
                    .Select(subscription => subscription.Id)
                    .Contains(link.CourseSubscriptionId))
                .ToListAsync(cancellationToken);
            var linkedPairs = existingLinks
                .Select(link => (link.CourseSubscriptionId, link.ExternalContentId))
                .ToHashSet();
            var scannedAtUtc = timeProvider.GetUtcNow();
            var reviewRequiredCount = 0;
            var newTaskEligibleCount = 0;

            foreach (var change in diff.Changes)
            {
                if (change.Kind == CourseContentChangeKind.Missing)
                {
                    existingById[change.Existing!.Id].MarkNotVisible();
                    continue;
                }

                var incoming = change.Incoming!;
                var (processingState, reviewReason) = Classify(incoming);
                if (processingState == ExternalContentProcessingState.ReviewRequired)
                {
                    reviewRequiredCount++;
                }

                ExternalContent content;
                if (change.Kind == CourseContentChangeKind.New)
                {
                    content = ExternalContent.Create(
                        target.CourseId,
                        incoming.ProviderContentId,
                        incoming.Kind,
                        incoming.Title,
                        incoming.Description,
                        incoming.SourceUri.AbsoluteUri,
                        incoming.StructuredDueDateUtc,
                        processingState,
                        reviewReason,
                        scannedAtUtc);
                    dbContext.ExternalContents.Add(content);
                }
                else
                {
                    content = existingById[change.Existing!.Id];
                    var wasTaskEligible =
                        content.ProcessingState == ExternalContentProcessingState.TaskEligible;
                    content.ApplySnapshot(
                        incoming.Kind,
                        incoming.Title,
                        incoming.Description,
                        incoming.SourceUri.AbsoluteUri,
                        incoming.StructuredDueDateUtc,
                        processingState,
                        reviewReason,
                        scannedAtUtc);

                    if (!wasTaskEligible
                        && processingState == ExternalContentProcessingState.TaskEligible)
                    {
                        newTaskEligibleCount++;
                    }
                }

                if (change.Kind == CourseContentChangeKind.New
                    && processingState == ExternalContentProcessingState.TaskEligible)
                {
                    newTaskEligibleCount++;
                }

                if (processingState != ExternalContentProcessingState.TaskEligible)
                {
                    continue;
                }

                foreach (var subscription in subscriptions)
                {
                    if (linkedPairs.Contains((subscription.Id, content.Id)))
                    {
                        continue;
                    }

                    var task = new StudyTask(
                        subscription.ModuleId,
                        incoming.Title,
                        incoming.StructuredDueDateUtc!.Value,
                        incoming.Description);
                    var link = new ExternalTaskLink(
                        subscription.Id,
                        content.Id,
                        task.Id,
                        scannedAtUtc);
                    dbContext.Tasks.Add(task);
                    dbContext.ExternalTaskLinks.Add(link);
                    linkedPairs.Add((subscription.Id, content.Id));
                }
            }

            scanRun.Succeed(scannedAtUtc);
            course.MarkScanSucceeded(scanRun.Id, scannedAtUtc);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            var summary = new CourseScanSummary(
                diff.Changes.Count(change => change.Kind == CourseContentChangeKind.New),
                diff.Changes.Count(change => change.Kind == CourseContentChangeKind.Changed),
                reviewRequiredCount,
                diff.Changes.Count(change => change.Kind == CourseContentChangeKind.Missing),
                newTaskEligibleCount);
            return new CourseScanResult(CourseScanOutcome.Succeeded, summary, null);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static bool IsValidSnapshot(
        CourseSnapshot? snapshot,
        ScanTarget target)
    {
        if (snapshot is null
            || !snapshot.IsComplete
            || !string.Equals(
                snapshot.ProviderKey,
                target.ProviderKey,
                StringComparison.Ordinal)
            || !string.Equals(
                snapshot.ExternalCourseId,
                target.ExternalCourseId,
                StringComparison.Ordinal)
            || snapshot.Contents is null)
        {
            return false;
        }

        var providerContentIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in snapshot.Contents)
        {
            if (item is null
                || string.IsNullOrWhiteSpace(item.ProviderContentId)
                || !providerContentIds.Add(item.ProviderContentId)
                || string.IsNullOrWhiteSpace(item.Title)
                || item.SourceUri is null
                || !item.SourceUri.IsAbsoluteUri
                || (!string.Equals(
                        item.SourceUri.Scheme,
                        Uri.UriSchemeHttp,
                        StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(
                        item.SourceUri.Scheme,
                        Uri.UriSchemeHttps,
                        StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }
        }

        return true;
    }

    private static CourseSnapshot CanonicalizeSnapshot(CourseSnapshot snapshot)
    {
        if (snapshot.Contents is null)
        {
            return snapshot;
        }

        return snapshot with
        {
            Contents = snapshot.Contents
                .Select(item => item is null
                    ? null!
                    : item with
                    {
                        ProviderContentId = item.ProviderContentId?.Trim()!
                    })
                .ToArray()
        };
    }

    private static (ExternalContentProcessingState State, ExternalContentReviewReason Reason)
        Classify(CourseSnapshotItem item) =>
            item.Kind != ExternalContentKind.Assignment
                ? (ExternalContentProcessingState.ReviewRequired,
                    ExternalContentReviewReason.NotAnAssignment)
                : item.StructuredDueDateUtc is null
                    ? (ExternalContentProcessingState.ReviewRequired,
                        ExternalContentReviewReason.MissingStructuredDeadline)
                    : (ExternalContentProcessingState.TaskEligible,
                        ExternalContentReviewReason.None);

    private sealed record ScanTarget(
        Guid CourseId,
        string ProviderKey,
        string ExternalCourseId);
}
