using Microsoft.EntityFrameworkCore;
using Npgsql;
using StudyOrganizer.Application.ExternalCourses;
using StudyOrganizer.Domain.ExternalCourses;
using StudyOrganizer.Domain.Tasks;
using StudyOrganizer.Infrastructure.Persistence;

namespace StudyOrganizer.Infrastructure.ExternalCourses;

public sealed class CourseSubscriptionHandler(
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    ICourseScanOrchestrator scanOrchestrator,
    IExternalCourseUrlResolver courseUrlResolver,
    TimeProvider timeProvider)
    : ICourseSubscriptionHandler
{
    public async Task<CourseSubscriptionRegistrationResult> RegisterAsync(
        Guid ownerId,
        Guid moduleId,
        string courseUrl,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await RegisterCoreAsync(
                ownerId,
                moduleId,
                courseUrl,
                cancellationToken);
        }
        catch (DbUpdateException exception)
            when (IsRegistrationConflict(exception))
        {
            return await RegisterCoreAsync(
                ownerId,
                moduleId,
                courseUrl,
                cancellationToken);
        }
    }

    private async Task<CourseSubscriptionRegistrationResult>
        RegisterCoreAsync(
        Guid ownerId,
        Guid moduleId,
        string courseUrl,
        CancellationToken cancellationToken)
    {
        var resolvedCourse = courseUrlResolver.Resolve(courseUrl);
        if (resolvedCourse is null)
        {
            return new CourseSubscriptionRegistrationResult(
                CourseSubscriptionRegistrationOutcome
                    .UnsupportedCourseUrl);
        }

        await using var context =
            await dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        var ownsModule = await context.Modules.AnyAsync(
            module =>
                module.Id == moduleId
                && module.OwnerId == ownerId,
            cancellationToken);

        if (!ownsModule)
        {
            return new CourseSubscriptionRegistrationResult(
                CourseSubscriptionRegistrationOutcome.NotFound);
        }

        var moduleSubscription = await context.CourseSubscriptions
            .SingleOrDefaultAsync(
                subscription => subscription.StudyModuleId == moduleId,
                cancellationToken);

        if (moduleSubscription is not null)
        {
            var subscribedCourse = await context.ExternalCourses
                .SingleAsync(
                    course =>
                        course.Id
                        == moduleSubscription.ExternalCourseId,
                    cancellationToken);

            if (subscribedCourse.Identity != resolvedCourse.Identity)
            {
                return new CourseSubscriptionRegistrationResult(
                    CourseSubscriptionRegistrationOutcome
                        .ModuleAlreadySubscribed);
            }

            if (moduleSubscription.State ==
                CourseSubscriptionState.Active)
            {
                return await RegistrationResultAsync(
                    ownerId,
                    moduleId,
                    CourseSubscriptionRegistrationOutcome.Completed,
                    cancellationToken);
            }

            var beganReactivation = moduleSubscription.State ==
                CourseSubscriptionState.Ended;
            if (beganReactivation)
            {
                moduleSubscription.BeginReactivation();
                await context.SaveChangesAsync(cancellationToken);
            }

            return await CompletePendingRegistrationAsync(
                ownerId,
                moduleId,
                moduleSubscription.Id,
                subscribedCourse.Id,
                forceSetupScan: beganReactivation,
                cancellationToken);
        }

        var course = await context.ExternalCourses.SingleOrDefaultAsync(
            candidate =>
                candidate.Identity.SourceType ==
                    resolvedCourse.Identity.SourceType
                && candidate.Identity.SourceInstance ==
                    resolvedCourse.Identity.SourceInstance
                && candidate.Identity.ExternalCourseKey ==
                    resolvedCourse.Identity.ExternalCourseKey,
            cancellationToken);

        if (course is not null)
        {
            var existingOwnerSubscription =
                await context.CourseSubscriptions.AnyAsync(
                    subscription =>
                        subscription.OwnerId == ownerId
                        && subscription.ExternalCourseId == course.Id,
                    cancellationToken);

            if (existingOwnerSubscription)
            {
                return new CourseSubscriptionRegistrationResult(
                    CourseSubscriptionRegistrationOutcome
                        .CourseAlreadySubscribed);
            }
        }
        else
        {
            course = new ExternalCourse(
                resolvedCourse.Identity,
                resolvedCourse.DisplayName,
                timeProvider.GetUtcNow());
            context.ExternalCourses.Add(course);
        }

        var subscription = new CourseSubscription(
            moduleId,
            ownerId,
            course.Id,
            timeProvider.GetUtcNow());
        context.CourseSubscriptions.Add(subscription);
        await context.SaveChangesAsync(cancellationToken);

        return await CompletePendingRegistrationAsync(
            ownerId,
            moduleId,
            subscription.Id,
            course.Id,
            forceSetupScan: false,
            cancellationToken);
    }

    public async Task<CourseSubscriptionResult?> GetAsync(
        Guid ownerId,
        Guid moduleId,
        CancellationToken cancellationToken = default)
    {
        var subscription = await GetAccessibleSubscriptionAsync(
            ownerId,
            moduleId,
            cancellationToken);
        if (subscription is null)
        {
            return null;
        }

        if (subscription.State == CourseSubscriptionState.Pending)
        {
            await ActivateFromCurrentSnapshotAsync(
                subscription.Id,
                cancellationToken);
        }

        return await BuildSubscriptionResultAsync(
            ownerId,
            moduleId,
            cancellationToken);
    }

    public async Task<CourseSubscriptionEndResult> EndAsync(
        Guid ownerId,
        Guid moduleId,
        CancellationToken cancellationToken = default)
    {
        await using var context =
            await dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        var ownsModule = await context.Modules.AnyAsync(
            module =>
                module.Id == moduleId
                && module.OwnerId == ownerId,
            cancellationToken);
        if (!ownsModule)
        {
            return CourseSubscriptionEndResult.NotFound;
        }

        var subscription = await context.CourseSubscriptions
            .SingleOrDefaultAsync(
                candidate =>
                    candidate.StudyModuleId == moduleId
                    && candidate.OwnerId == ownerId,
                cancellationToken);
        if (subscription is null
            || subscription.State == CourseSubscriptionState.Ended)
        {
            return CourseSubscriptionEndResult.Ended;
        }

        await using var transaction =
            await context.Database.BeginTransactionAsync(
                cancellationToken);

        subscription.End(timeProvider.GetUtcNow());

        var stateIds = await context.SubscriptionContentStates
            .Where(state =>
                state.CourseSubscriptionId == subscription.Id)
            .Select(state => state.Id)
            .ToListAsync(cancellationToken);
        var sourceUpdates = await context.SourceUpdates
            .Where(update =>
                stateIds.Contains(update.SubscriptionContentStateId))
            .ToListAsync(cancellationToken);
        context.SourceUpdates.RemoveRange(sourceUpdates);

        var hasOtherActiveSubscription =
            await context.CourseSubscriptions.AnyAsync(
                candidate =>
                    candidate.ExternalCourseId ==
                        subscription.ExternalCourseId
                    && candidate.Id != subscription.Id
                    && candidate.State == CourseSubscriptionState.Active,
                cancellationToken);

        if (!hasOtherActiveSubscription)
        {
            var course = await context.ExternalCourses.SingleAsync(
                candidate =>
                    candidate.Id == subscription.ExternalCourseId,
                cancellationToken);
            course.Deactivate(timeProvider.GetUtcNow());
        }

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return CourseSubscriptionEndResult.Ended;
    }

    public async Task<CourseScanRequestResult> StartScanAsync(
        Guid ownerId,
        Guid moduleId,
        CancellationToken cancellationToken = default)
    {
        var subscription = await GetAccessibleSubscriptionAsync(
            ownerId,
            moduleId,
            cancellationToken);
        if (subscription is null)
        {
            return new CourseScanRequestResult(
                CourseScanRequestOutcome.NotFound);
        }

        if (subscription.State == CourseSubscriptionState.Pending
            && await ActivateFromCurrentSnapshotAsync(
                subscription.Id,
                cancellationToken))
        {
            subscription = await GetAccessibleSubscriptionAsync(
                ownerId,
                moduleId,
                cancellationToken)
                ?? throw new InvalidOperationException(
                    "An activated Course Subscription must remain accessible.");
        }

        var activationSubscriptionId =
            subscription.State == CourseSubscriptionState.Pending
                ? subscription.Id
                : (Guid?)null;

        var scanResult = await scanOrchestrator.ScanAsync(
            subscription.ExternalCourseId,
            activationSubscriptionId,
            CancellationToken.None);

        var details = await BuildScanResultAsync(
            subscription.Id,
            scanResult.ScanRunId,
            CancellationToken.None)
            ?? throw new InvalidOperationException(
                "A persisted Scan Run must be readable.");

        return new CourseScanRequestResult(
            details.Status == ScanRunStatus.Running
                ? CourseScanRequestOutcome.Running
                : CourseScanRequestOutcome.Completed,
            details);
    }

    public async Task<CourseScanResultDetails?> GetScanAsync(
        Guid ownerId,
        Guid moduleId,
        Guid scanRunId,
        CancellationToken cancellationToken = default)
    {
        var subscription = await GetAccessibleSubscriptionAsync(
            ownerId,
            moduleId,
            cancellationToken);
        if (subscription is null)
        {
            return null;
        }

        if (subscription.State == CourseSubscriptionState.Pending)
        {
            await ActivateFromCurrentSnapshotAsync(
                subscription.Id,
                cancellationToken);
            subscription = await GetAccessibleSubscriptionAsync(
                ownerId,
                moduleId,
                cancellationToken)
                ?? subscription;
        }

        await using var context =
            await dbContextFactory.CreateDbContextAsync(
                cancellationToken);
        var scan = await context.ScanRuns
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate =>
                    candidate.Id == scanRunId
                    && candidate.ExternalCourseId ==
                        subscription.ExternalCourseId,
                cancellationToken);
        if (scan is null
            || subscription.State == CourseSubscriptionState.Active
                && !IsVisibleToActiveSubscription(scan, subscription))
        {
            return null;
        }

        if (subscription.State == CourseSubscriptionState.Pending)
        {
            var latestScanId = await context.ScanRuns
                .AsNoTracking()
                .Where(candidate =>
                    candidate.ExternalCourseId ==
                        subscription.ExternalCourseId)
                .OrderByDescending(candidate => candidate.StartedAt)
                .Select(candidate => candidate.Id)
                .FirstOrDefaultAsync(cancellationToken);
            if (latestScanId != scanRunId)
            {
                return null;
            }
        }

        return await BuildScanResultAsync(
            subscription.Id,
            scanRunId,
            cancellationToken);
    }

    private async Task<CourseSubscriptionRegistrationResult>
        CompletePendingRegistrationAsync(
            Guid ownerId,
            Guid moduleId,
            Guid subscriptionId,
            Guid externalCourseId,
            bool forceSetupScan,
            CancellationToken cancellationToken)
    {
        if (await ActivateFromCurrentSnapshotAsync(
                subscriptionId,
                cancellationToken))
        {
            return await RegistrationResultAsync(
                ownerId,
                moduleId,
                CourseSubscriptionRegistrationOutcome.Completed,
                cancellationToken);
        }

        if (!forceSetupScan)
        {
            await using var context =
                await dbContextFactory.CreateDbContextAsync(
                    cancellationToken);
            var subscription = await context.CourseSubscriptions
                .AsNoTracking()
                .SingleAsync(
                    candidate => candidate.Id == subscriptionId,
                    cancellationToken);
            var latestScan = await context.ScanRuns
                .AsNoTracking()
                .Where(scan =>
                    scan.ExternalCourseId == externalCourseId
                    && scan.StartedAt >= subscription.CreatedAt)
                .OrderByDescending(scan => scan.StartedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (latestScan is not null
                && latestScan.Status != ScanRunStatus.Running)
            {
                return await RegistrationResultAsync(
                    ownerId,
                    moduleId,
                    CourseSubscriptionRegistrationOutcome.Completed,
                    cancellationToken);
            }
        }

        var scanResult = await scanOrchestrator.ScanAsync(
            externalCourseId,
            subscriptionId,
            CancellationToken.None);

        return await RegistrationResultAsync(
            ownerId,
            moduleId,
            scanResult.Status == ScanRunStatus.Running
                ? CourseSubscriptionRegistrationOutcome.Running
                : CourseSubscriptionRegistrationOutcome.Completed,
            CancellationToken.None);
    }

    private async Task<CourseSubscriptionRegistrationResult>
        RegistrationResultAsync(
            Guid ownerId,
            Guid moduleId,
            CourseSubscriptionRegistrationOutcome outcome,
            CancellationToken cancellationToken)
    {
        var result = await BuildSubscriptionResultAsync(
            ownerId,
            moduleId,
            cancellationToken)
            ?? throw new InvalidOperationException(
                "A registered Course Subscription must be readable.");

        return new CourseSubscriptionRegistrationResult(
            outcome,
            result);
    }

    private async Task<bool> ActivateFromCurrentSnapshotAsync(
        Guid subscriptionId,
        CancellationToken cancellationToken)
    {
        await using var context =
            await dbContextFactory.CreateDbContextAsync(
                cancellationToken);
        await using var transaction =
            await context.Database.BeginTransactionAsync(
                cancellationToken);

        var subscription = await context.CourseSubscriptions
            .SingleAsync(
                candidate => candidate.Id == subscriptionId,
                cancellationToken);
        if (subscription.State != CourseSubscriptionState.Pending)
        {
            return subscription.State == CourseSubscriptionState.Active;
        }

        var course = await context.ExternalCourses.SingleAsync(
            candidate => candidate.Id == subscription.ExternalCourseId,
            cancellationToken);
        if (course.State != ExternalCourseState.Active)
        {
            return false;
        }

        var snapshot = await context.CourseSnapshots
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate =>
                    candidate.ExternalCourseId == course.Id
                    && candidate.IsCurrent,
                cancellationToken);
        if (snapshot is null)
        {
            return false;
        }

        var contentIds = await context.CourseSnapshotItems
            .AsNoTracking()
            .Where(item => item.CourseSnapshotId == snapshot.Id)
            .Select(item => item.ExternalLearningContentId)
            .ToListAsync(cancellationToken);
        var contents = await context.ExternalLearningContents
            .Where(content => contentIds.Contains(content.Id))
            .ToListAsync(cancellationToken);
        var states = await context.SubscriptionContentStates
            .Where(state =>
                state.CourseSubscriptionId == subscription.Id)
            .ToListAsync(cancellationToken);
        var stateIds = states.Select(state => state.Id).ToList();
        var updates = await context.SourceUpdates
            .Where(update =>
                stateIds.Contains(update.SubscriptionContentStateId))
            .ToDictionaryAsync(
                update => update.SubscriptionContentStateId,
                cancellationToken);

        var now = timeProvider.GetUtcNow();
        var activationAt = subscription.CreatedAt <= snapshot.ObservedAt
            ? snapshot.ObservedAt
            : now;

        foreach (var content in contents)
        {
            var state = states.SingleOrDefault(candidate =>
                candidate.ExternalLearningContentId == content.Id);
            if (state is null)
            {
                var task = new StudyTask(
                    subscription.StudyModuleId,
                    content.Title,
                    content.DueDate,
                    null,
                    activationAt);
                context.Tasks.Add(task);
                context.SubscriptionContentStates.Add(
                    new SubscriptionContentState(
                        subscription.Id,
                        course.Id,
                        content.Id,
                        task.Id,
                        CopySignature(content),
                        activationAt));
                continue;
            }

            if (state.Status != SubscriptionContentStateStatus.Imported
                || state.ConfirmedSignature == content.Signature)
            {
                continue;
            }

            if (updates.TryGetValue(state.Id, out var update))
            {
                update.Refresh(CopySignature(content), activationAt);
            }
            else
            {
                context.SourceUpdates.Add(
                    new SourceUpdate(
                        state.Id,
                        CopySignature(content),
                        activationAt));
            }
        }

        subscription.Activate(activationAt);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    private async Task<CourseSubscription?> GetAccessibleSubscriptionAsync(
        Guid ownerId,
        Guid moduleId,
        CancellationToken cancellationToken)
    {
        await using var context =
            await dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        return await context.CourseSubscriptions
            .AsNoTracking()
            .SingleOrDefaultAsync(
                subscription =>
                    subscription.StudyModuleId == moduleId
                    && subscription.OwnerId == ownerId
                    && subscription.State != CourseSubscriptionState.Ended
                    && context.Modules.Any(module =>
                        module.Id == moduleId
                        && module.OwnerId == ownerId),
                cancellationToken);
    }

    private async Task<CourseSubscriptionResult?>
        BuildSubscriptionResultAsync(
            Guid ownerId,
            Guid moduleId,
            CancellationToken cancellationToken)
    {
        await using var context =
            await dbContextFactory.CreateDbContextAsync(
                cancellationToken);
        var subscription = await context.CourseSubscriptions
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate =>
                    candidate.StudyModuleId == moduleId
                    && candidate.OwnerId == ownerId
                    && candidate.State != CourseSubscriptionState.Ended,
                cancellationToken);
        if (subscription is null)
        {
            return null;
        }

        var course = await context.ExternalCourses
            .AsNoTracking()
            .SingleAsync(
                candidate =>
                    candidate.Id == subscription.ExternalCourseId,
                cancellationToken);

        CourseSnapshotSummaryResult? snapshotResult = null;
        if (subscription.State == CourseSubscriptionState.Active)
        {
            var snapshot = await context.CourseSnapshots
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    candidate =>
                        candidate.ExternalCourseId == course.Id
                        && candidate.IsCurrent,
                    cancellationToken);
            if (snapshot is not null)
            {
                var contentCount =
                    await context.CourseSnapshotItems.CountAsync(
                        item => item.CourseSnapshotId == snapshot.Id,
                        cancellationToken);
                snapshotResult = new CourseSnapshotSummaryResult(
                    snapshot.ObservedAt,
                    contentCount);
            }
        }

        var scansQuery = context.ScanRuns
            .AsNoTracking()
            .Where(scan => scan.ExternalCourseId == course.Id);

        if (subscription.State == CourseSubscriptionState.Active
            && subscription.ActivatedAt.HasValue)
        {
            var activatedAt = subscription.ActivatedAt.Value;
            scansQuery = scansQuery.Where(scan =>
                scan.Status == ScanRunStatus.Running
                    ? scan.StartedAt >= activatedAt
                    : scan.CompletedAt >= activatedAt);
        }
        else
        {
            scansQuery = scansQuery.Where(scan =>
                scan.StartedAt >= subscription.CreatedAt
                || scan.Status == ScanRunStatus.Running);
        }

        var maximumScanCount = subscription.State ==
            CourseSubscriptionState.Active
            ? 10
            : 1;
        var scanIds = await scansQuery
            .OrderByDescending(scan => scan.StartedAt)
            .Take(maximumScanCount)
            .Select(scan => scan.Id)
            .ToListAsync(cancellationToken);
        var recentScans = new List<CourseScanResultDetails>(scanIds.Count);
        foreach (var scanId in scanIds)
        {
            var scan = await BuildScanResultAsync(
                subscription.Id,
                scanId,
                cancellationToken);
            if (scan is not null)
            {
                recentScans.Add(scan);
            }
        }

        var resolved = courseUrlResolver.Resolve(
            $"{course.Identity.SourceInstance}/course/"
            + Uri.EscapeDataString(
                course.Identity.ExternalCourseKey));

        return new CourseSubscriptionResult(
            moduleId,
            subscription.State,
            subscription.CreatedAt,
            subscription.ActivatedAt,
            new ExternalCourseSummaryResult(
                course.Name,
                course.Identity.SourceType,
                resolved?.SourceUrl),
            snapshotResult,
            recentScans.FirstOrDefault(),
            recentScans);
    }

    private async Task<CourseScanResultDetails?> BuildScanResultAsync(
        Guid subscriptionId,
        Guid scanRunId,
        CancellationToken cancellationToken)
    {
        await using var context =
            await dbContextFactory.CreateDbContextAsync(
                cancellationToken);
        var scan = await context.ScanRuns
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate => candidate.Id == scanRunId,
                cancellationToken);
        if (scan is null)
        {
            return null;
        }

        var tasksCreated = 0;
        var pdfTasksCreated = 0;
        var sourceUpdatesCreated = 0;

        if (scan.CompletedAt.HasValue)
        {
            var importedContent = await (
                from state in context.SubscriptionContentStates.AsNoTracking()
                join content in context.ExternalLearningContents.AsNoTracking()
                    on state.ExternalLearningContentId equals content.Id
                where state.CourseSubscriptionId == subscriptionId
                    && state.CreatedAt == scan.CompletedAt.Value
                select new
                {
                    content.Type,
                    content.MediaType
                })
                .ToListAsync(cancellationToken);

            tasksCreated = importedContent.Count;
            pdfTasksCreated = importedContent.Count(content =>
                content.Type == ExternalLearningContentType.File
                && string.Equals(
                    content.MediaType,
                    "application/pdf",
                    StringComparison.OrdinalIgnoreCase));

            sourceUpdatesCreated = await (
                from update in context.SourceUpdates.AsNoTracking()
                join state in context.SubscriptionContentStates.AsNoTracking()
                    on update.SubscriptionContentStateId equals state.Id
                where state.CourseSubscriptionId == subscriptionId
                    && update.DetectedByScanRunId == scan.Id
                select update.Id)
                .CountAsync(cancellationToken);
        }

        return new CourseScanResultDetails(
            scan.Id,
            scan.Status,
            scan.StartedAt,
            scan.CompletedAt,
            scan.Counts,
            new CourseScanPersonalImpactResult(
                tasksCreated,
                pdfTasksCreated,
                tasksCreated - pdfTasksCreated,
                sourceUpdatesCreated),
            scan.ErrorCode,
            scan.Status is ScanRunStatus.Failed
                or ScanRunStatus.Cancelled
                or ScanRunStatus.Expired);
    }

    private static bool IsVisibleToActiveSubscription(
        ScanRun scan,
        CourseSubscription subscription)
    {
        if (!subscription.ActivatedAt.HasValue)
        {
            return false;
        }

        return scan.Status == ScanRunStatus.Running
            ? scan.StartedAt >= subscription.ActivatedAt.Value
            : scan.CompletedAt >= subscription.ActivatedAt.Value;
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

    private static bool IsRegistrationConflict(
        DbUpdateException exception)
    {
        return exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName:
                "ux_external_courses_identity"
                or "ux_course_subscriptions_study_module_id"
                or "ux_course_subscriptions_owner_course"
                or "ux_subscription_content_states_subscription_content"
        };
    }
}
