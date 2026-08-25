using Microsoft.EntityFrameworkCore;
using Npgsql;
using StudyOrganizer.Application.ExternalCourses;
using StudyOrganizer.Domain.ExternalCourses;
using StudyOrganizer.Domain.Tasks;
using StudyOrganizer.Infrastructure.Persistence;

namespace StudyOrganizer.Infrastructure.ExternalCourses;

public sealed class CourseScanOrchestrator(
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    IExternalCourseSource courseSource,
    TimeProvider timeProvider,
    CourseScanOptions options)
    : ICourseScanOrchestrator
{
    public async Task<CourseScanResult> ScanAsync(
        Guid externalCourseId,
        Guid? activationSubscriptionId = null,
        CancellationToken cancellationToken = default)
    {
        if (externalCourseId == Guid.Empty)
        {
            throw new ArgumentException(
                "External Course ID must not be empty.",
                nameof(externalCourseId));
        }

        var now = timeProvider.GetUtcNow();
        ScanRun scanRun;
        ExternalCourseIdentity identity;

        await using (var claimContext =
            await dbContextFactory.CreateDbContextAsync(
                cancellationToken))
        {
            var course = await claimContext.ExternalCourses
                .SingleAsync(
                    item => item.Id == externalCourseId,
                    cancellationToken);
            identity = course.Identity;

            var runningScan = await claimContext.ScanRuns
                .SingleOrDefaultAsync(
                    scan =>
                        scan.ExternalCourseId == externalCourseId
                        && scan.Status == ScanRunStatus.Running,
                    cancellationToken);

            if (runningScan is not null)
            {
                if (runningScan.LeaseExpiresAt > now)
                {
                    return ToResult(runningScan, true);
                }

                runningScan.Expire(now);
                await claimContext.SaveChangesAsync(
                    cancellationToken);
            }

            scanRun = new ScanRun(
                externalCourseId,
                now,
                now.Add(options.LeaseDuration),
                activationSubscriptionId);
            claimContext.ScanRuns.Add(scanRun);
            try
            {
                await claimContext.SaveChangesAsync(
                    cancellationToken);
            }
            catch (DbUpdateException exception)
                when (IsRunningScanConflict(exception))
            {
                await using var conflictContext =
                    await dbContextFactory.CreateDbContextAsync(
                        cancellationToken);
                var winningScan = await conflictContext.ScanRuns
                    .Where(scan =>
                        scan.ExternalCourseId == externalCourseId)
                    .OrderByDescending(scan => scan.StartedAt)
                    .FirstAsync(cancellationToken);
                return ToResult(winningScan, true);
            }
        }

        CourseSourceSnapshot sourceSnapshot;
        try
        {
            sourceSnapshot = await courseSource
                .FetchSnapshotAsync(identity, cancellationToken)
                .WaitAsync(
                    options.Timeout,
                    timeProvider,
                    cancellationToken);
        }
        catch (TimeoutException)
        {
            return await FailScanAsync(
                scanRun.Id,
                ScanRunErrorCode.Timeout,
                CancellationToken.None);
        }
        catch (ExternalCourseSourceException exception)
        {
            return await FailScanAsync(
                scanRun.Id,
                exception.ErrorCode,
                CancellationToken.None);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            return await CancelScanAsync(scanRun.Id);
        }
        catch (Exception)
        {
            return await FailScanAsync(
                scanRun.Id,
                ScanRunErrorCode.Unexpected,
                CancellationToken.None);
        }

        if (!IsValid(sourceSnapshot))
        {
            return await FailScanAsync(
                scanRun.Id,
                ScanRunErrorCode.InvalidSourceData,
                cancellationToken);
        }

        try
        {
        await using var persistenceContext =
            await dbContextFactory.CreateDbContextAsync(
                cancellationToken);
        await using var transaction =
            await persistenceContext.Database
                .BeginTransactionAsync(cancellationToken);

        scanRun = await persistenceContext.ScanRuns.SingleAsync(
            scan => scan.Id == scanRun.Id,
            cancellationToken);
        var completedAt = timeProvider.GetUtcNow();

        if (scanRun.Status != ScanRunStatus.Running)
        {
            return ToResult(scanRun, false);
        }

        if (completedAt >= scanRun.LeaseExpiresAt)
        {
            scanRun.Expire(completedAt);
            await persistenceContext.SaveChangesAsync(
                cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return ToResult(scanRun, false);
        }

        var persistenceCourse = await persistenceContext.ExternalCourses
            .SingleAsync(
                candidate => candidate.Id == externalCourseId,
                cancellationToken);
        var courseSubscriptions = await persistenceContext
            .CourseSubscriptions
            .Where(subscription =>
                subscription.ExternalCourseId == externalCourseId)
            .ToListAsync(cancellationToken);

        if (activationSubscriptionId.HasValue)
        {
            var activationSubscription = courseSubscriptions.Single(
                subscription =>
                    subscription.Id == activationSubscriptionId.Value);
            var hasOtherActiveSubscription = courseSubscriptions.Any(
                subscription =>
                    subscription.Id != activationSubscription.Id
                    && subscription.State ==
                        CourseSubscriptionState.Active);

            if (activationSubscription.State ==
                    CourseSubscriptionState.Ended
                && !hasOtherActiveSubscription)
            {
                scanRun.Cancel(completedAt);
                await persistenceContext.SaveChangesAsync(
                    cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return ToResult(scanRun, false);
            }

            if (activationSubscription.State ==
                CourseSubscriptionState.Pending)
            {
                activationSubscription.Activate(completedAt);
            }

            persistenceCourse.Activate();
        }

        var currentSnapshot = await persistenceContext
            .CourseSnapshots
            .SingleOrDefaultAsync(
                candidate =>
                    candidate.ExternalCourseId == externalCourseId
                    && candidate.IsCurrent,
                cancellationToken);
        if (currentSnapshot is not null)
        {
            currentSnapshot.MarkSuperseded();
            await persistenceContext.SaveChangesAsync(
                cancellationToken);
        }

        var snapshot = new CourseSnapshot(
            externalCourseId,
            scanRun.Id,
            completedAt);
        persistenceContext.CourseSnapshots.Add(snapshot);

        var subscriptions = courseSubscriptions
            .Where(subscription =>
                subscription.State == CourseSubscriptionState.Active)
            .ToList();

        var contents = (await persistenceContext
                .ExternalLearningContents
                .Where(content =>
                    content.ExternalCourseId == externalCourseId)
                .ToListAsync(cancellationToken))
            .ToDictionary(
                content => content.ExternalContentKey.Value,
                StringComparer.Ordinal);
        var importStates = await persistenceContext
            .SubscriptionContentStates
            .Where(state => state.ExternalCourseId == externalCourseId)
            .ToListAsync(cancellationToken);
        var sourceUpdates = (await persistenceContext.SourceUpdates
                .Where(update => importStates
                    .Select(state => state.Id)
                    .Contains(update.SubscriptionContentStateId))
                .ToListAsync(cancellationToken))
            .ToDictionary(
                update => update.SubscriptionContentStateId);

        var newCount = 0;
        var updatedCount = 0;
        var unchangedCount = 0;
        var unavailableCount = 0;
        var observedKeys = new HashSet<string>(
            StringComparer.Ordinal);

        foreach (var sourceItem in sourceSnapshot.Items)
        {
            observedKeys.Add(sourceItem.ExternalContentKey.Value);

            if (!contents.TryGetValue(
                sourceItem.ExternalContentKey.Value,
                out var content))
            {
                content = new ExternalLearningContent(
                    externalCourseId,
                    sourceItem.ExternalContentKey,
                    sourceItem.Type,
                    sourceItem.Title,
                    completedAt,
                    sourceItem.DueDate,
                    sourceItem.MediaType,
                    sourceItem.SourceReference);
                persistenceContext.ExternalLearningContents.Add(content);
                contents.Add(
                    content.ExternalContentKey.Value,
                    content);
                newCount++;
            }
            else
            {
                var observedSignature = ContentSignature.Compute(
                    sourceItem.Type,
                    sourceItem.Title,
                    sourceItem.DueDate,
                    sourceItem.MediaType,
                    sourceItem.SourceReference,
                    ExternalLearningContentAvailability.Available);

                if (content.Signature == observedSignature)
                {
                    unchangedCount++;
                }
                else
                {
                    content.UpdateMetadata(
                        sourceItem.Type,
                        sourceItem.Title,
                        sourceItem.DueDate,
                        sourceItem.MediaType,
                        sourceItem.SourceReference,
                        completedAt);
                    content.MarkAvailable(completedAt);
                    updatedCount++;
                }
            }

            persistenceContext.CourseSnapshotItems.Add(
                new CourseSnapshotItem(
                    snapshot.Id,
                    externalCourseId,
                    content.Id,
                    content.ExternalContentKey,
                    content.Type,
                    content.Title,
                    content.DueDate,
                    content.MediaType,
                    content.SourceReference));

            foreach (var subscription in subscriptions)
            {
                var importState = importStates.SingleOrDefault(state =>
                    state.CourseSubscriptionId == subscription.Id
                    && state.ExternalLearningContentId == content.Id);
                if (importState is not null)
                {
                    if (importState.Status ==
                            SubscriptionContentStateStatus.Imported
                        && importState.ConfirmedSignature !=
                            content.Signature)
                    {
                        if (sourceUpdates.TryGetValue(
                            importState.Id,
                            out var sourceUpdate))
                        {
                            sourceUpdate.Refresh(
                                CopySignature(content),
                                completedAt,
                                scanRun.Id);
                        }
                        else
                        {
                            sourceUpdate = new SourceUpdate(
                                importState.Id,
                                CopySignature(content),
                                completedAt,
                                scanRun.Id);
                            persistenceContext.SourceUpdates.Add(
                                sourceUpdate);
                            sourceUpdates.Add(
                                importState.Id,
                                sourceUpdate);
                        }
                    }
                    else if (sourceUpdates.TryGetValue(
                        importState.Id,
                        out var obsoleteUpdate))
                    {
                        persistenceContext.SourceUpdates.Remove(
                            obsoleteUpdate);
                        sourceUpdates.Remove(importState.Id);
                    }

                    continue;
                }

                var task = new StudyTask(
                    subscription.StudyModuleId,
                    content.Title,
                    content.DueDate,
                    null,
                    completedAt);
                persistenceContext.Tasks.Add(task);
                persistenceContext.SubscriptionContentStates.Add(
                    new SubscriptionContentState(
                        subscription.Id,
                        externalCourseId,
                        content.Id,
                        task.Id,
                        CopySignature(content),
                        completedAt));
            }
        }

        foreach (var content in contents.Values.Where(content =>
            !observedKeys.Contains(content.ExternalContentKey.Value)
            && content.Availability ==
                ExternalLearningContentAvailability.Available))
        {
            content.MarkUnavailable(completedAt);
            unavailableCount++;

            foreach (var subscription in subscriptions)
            {
                var importState = importStates.SingleOrDefault(state =>
                    state.CourseSubscriptionId == subscription.Id
                    && state.ExternalLearningContentId == content.Id);
                if (importState is null
                    || importState.Status !=
                        SubscriptionContentStateStatus.Imported
                    || importState.ConfirmedSignature == content.Signature)
                {
                    continue;
                }

                if (sourceUpdates.TryGetValue(
                    importState.Id,
                    out var sourceUpdate))
                {
                    sourceUpdate.Refresh(
                        CopySignature(content),
                        completedAt,
                        scanRun.Id);
                }
                else
                {
                    sourceUpdate = new SourceUpdate(
                        importState.Id,
                        CopySignature(content),
                        completedAt,
                        scanRun.Id);
                    persistenceContext.SourceUpdates.Add(sourceUpdate);
                    sourceUpdates.Add(importState.Id, sourceUpdate);
                }
            }
        }

        scanRun.Succeed(
            new ScanRunCounts(
                newCount,
                updatedCount,
                unchangedCount,
                unavailableCount),
            completedAt);

        await persistenceContext.SaveChangesAsync(
            cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return ToResult(scanRun, false);
        }
        catch (DbUpdateException)
        {
            return await FailScanAsync(
                scanRun.Id,
                ScanRunErrorCode.PersistenceConflict,
                CancellationToken.None);
        }
    }

    private static CourseScanResult ToResult(
        ScanRun scanRun,
        bool reusedExistingRun)
    {
        return new CourseScanResult(
            scanRun.Id,
            scanRun.Status,
            scanRun.Counts,
            scanRun.ErrorCode,
            reusedExistingRun);
    }

    private async Task<CourseScanResult> FailScanAsync(
        Guid scanRunId,
        ScanRunErrorCode errorCode,
        CancellationToken cancellationToken)
    {
        await using var context =
            await dbContextFactory.CreateDbContextAsync(
                cancellationToken);
        var scanRun = await context.ScanRuns.SingleAsync(
            scan => scan.Id == scanRunId,
            cancellationToken);
        if (scanRun.Status != ScanRunStatus.Running)
        {
            return ToResult(scanRun, false);
        }

        scanRun.Fail(errorCode, timeProvider.GetUtcNow());
        await context.SaveChangesAsync(cancellationToken);
        return ToResult(scanRun, false);
    }

    private async Task<CourseScanResult> CancelScanAsync(
        Guid scanRunId)
    {
        await using var context =
            await dbContextFactory.CreateDbContextAsync(
                CancellationToken.None);
        var scanRun = await context.ScanRuns.SingleAsync(
            scan => scan.Id == scanRunId,
            CancellationToken.None);
        if (scanRun.Status != ScanRunStatus.Running)
        {
            return ToResult(scanRun, false);
        }

        scanRun.Cancel(timeProvider.GetUtcNow());
        await context.SaveChangesAsync(CancellationToken.None);
        return ToResult(scanRun, false);
    }

    private static bool IsValid(
        CourseSourceSnapshot? snapshot)
    {
        if (snapshot?.Items is null)
        {
            return false;
        }

        var keys = new HashSet<string>(StringComparer.Ordinal);

        foreach (var item in snapshot.Items)
        {
            if (item is null
                || item.ExternalContentKey is null
                || string.IsNullOrWhiteSpace(
                    item.ExternalContentKey.Value)
                || item.ExternalContentKey.Value.Length > 512
                || !Enum.IsDefined(item.Type)
                || string.IsNullOrWhiteSpace(item.Title)
                || item.Title.Trim().Length > 500
                || item.MediaType?.Trim().Length > 255
                || item.SourceReference?.Trim().Length > 2048
                || !keys.Add(item.ExternalContentKey.Value))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsRunningScanConflict(
        DbUpdateException exception)
    {
        return exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: "ux_scan_runs_running_course"
        };
    }

    private static ContentSignature CopySignature(
        ExternalLearningContent content)
    {
        return ContentSignature.Compute(
            content.Type,
            content.Title,
            content.DueDate,
            content.MediaType,
            content.SourceReference,
            content.Availability);
    }
}
