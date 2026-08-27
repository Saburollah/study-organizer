using Microsoft.EntityFrameworkCore;
using StudyOrganizer.Domain.ExternalCourses;
using StudyOrganizer.Infrastructure.Persistence;

namespace StudyOrganizer.Infrastructure.ExternalCourses;

public sealed class ExternalCourseCleanup(
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    ExternalCourseCleanupOptions options,
    TimeProvider timeProvider)
{
    public async Task<int> CleanupExpiredAsync(
        CancellationToken cancellationToken = default)
    {
        var cutoff = timeProvider.GetUtcNow() - options.RetentionPeriod;
        List<Guid> expiredCourseIds;
        await using (var context =
            await dbContextFactory.CreateDbContextAsync(cancellationToken))
        {
            expiredCourseIds = await context.ExternalCourses
                .AsNoTracking()
                .Where(course =>
                    course.State == ExternalCourseState.Inactive
                    && course.InactiveSince.HasValue
                    && course.InactiveSince.Value <= cutoff)
                .Select(course => course.Id)
                .ToListAsync(cancellationToken);
        }

        var cleanedCourseCount = 0;
        foreach (var courseId in expiredCourseIds)
        {
            if (await CleanupCourseAsync(
                    courseId,
                    cutoff,
                    cancellationToken))
            {
                cleanedCourseCount++;
            }
        }

        return cleanedCourseCount;
    }

    private async Task<bool> CleanupCourseAsync(
        Guid courseId,
        DateTimeOffset cutoff,
        CancellationToken cancellationToken)
    {
        await using var context =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction =
            await context.Database.BeginTransactionAsync(cancellationToken);

        await context.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM external_courses WHERE id = {courseId} FOR UPDATE",
            cancellationToken);
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM course_subscriptions WHERE external_course_id = {courseId} FOR UPDATE",
            cancellationToken);

        var course = await context.ExternalCourses.SingleOrDefaultAsync(
            candidate => candidate.Id == courseId,
            cancellationToken);
        if (course is null
            || course.State != ExternalCourseState.Inactive
            || !course.InactiveSince.HasValue
            || course.InactiveSince.Value > cutoff)
        {
            return false;
        }

        var hasAccessGrant = await context.CourseSubscriptions.AnyAsync(
            subscription =>
                subscription.ExternalCourseId == courseId
                && subscription.State != CourseSubscriptionState.Ended,
            cancellationToken);
        var hasRunningScan = await context.ScanRuns.AnyAsync(
            scan =>
                scan.ExternalCourseId == courseId
                && scan.Status == ScanRunStatus.Running,
            cancellationToken);
        if (hasAccessGrant || hasRunningScan)
        {
            return false;
        }

        var referencedContentIds =
            await context.SubscriptionContentStates
                .Where(state => state.ExternalCourseId == courseId)
                .Select(state => state.ExternalLearningContentId)
                .Distinct()
                .ToListAsync(cancellationToken);

        var hasTransientData = await context.CourseSnapshots.AnyAsync(
                snapshot => snapshot.ExternalCourseId == courseId,
                cancellationToken)
            || await context.ScanRuns.AnyAsync(
                scan => scan.ExternalCourseId == courseId,
                cancellationToken)
            || await context.ExternalLearningContents.AnyAsync(
                content =>
                    content.ExternalCourseId == courseId
                    && (!referencedContentIds.Contains(content.Id)
                        || !content.MetadataPurgedAt.HasValue),
                cancellationToken);
        if (!hasTransientData)
        {
            return false;
        }

        var stateIds = await context.SubscriptionContentStates
            .Where(state => state.ExternalCourseId == courseId)
            .Select(state => state.Id)
            .ToListAsync(cancellationToken);
        await context.SourceUpdates
            .Where(update =>
                stateIds.Contains(update.SubscriptionContentStateId))
            .ExecuteDeleteAsync(cancellationToken);

        await context.CourseSnapshotItems
            .Where(item => item.ExternalCourseId == courseId)
            .ExecuteDeleteAsync(cancellationToken);
        await context.CourseSnapshots
            .Where(snapshot => snapshot.ExternalCourseId == courseId)
            .ExecuteDeleteAsync(cancellationToken);
        await context.ScanRuns
            .Where(scan => scan.ExternalCourseId == courseId)
            .ExecuteDeleteAsync(cancellationToken);
        await context.ExternalLearningContents
            .Where(content =>
                content.ExternalCourseId == courseId
                && !referencedContentIds.Contains(content.Id))
            .ExecuteDeleteAsync(cancellationToken);

        if (referencedContentIds.Count == 0)
        {
            await context.CourseSubscriptions
                .Where(subscription =>
                    subscription.ExternalCourseId == courseId)
                .ExecuteDeleteAsync(cancellationToken);
            context.ExternalCourses.Remove(course);
        }
        else
        {
            var referencedContents =
                await context.ExternalLearningContents
                    .Where(content =>
                        content.ExternalCourseId == courseId
                        && referencedContentIds.Contains(content.Id))
                    .ToListAsync(cancellationToken);
            var now = timeProvider.GetUtcNow();
            foreach (var content in referencedContents)
            {
                content.PurgeMetadata(now);
            }
        }

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }
}
