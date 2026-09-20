using Microsoft.EntityFrameworkCore;
using StudyOrganizer.Application.ExternalCourses;
using StudyOrganizer.Domain.ExternalCourses;
using StudyOrganizer.Infrastructure.Persistence;

namespace StudyOrganizer.Infrastructure.ExternalCourses;

public sealed class ExternalCourseQueryHandler(
    ApplicationDbContext dbContext)
    : IExternalCourseQueryHandler
{
    public async Task<IReadOnlyList<CourseSubscriptionResult>> GetByOwnerAsync(
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        var subscriptions = await (
                from subscription in dbContext.CourseSubscriptions.AsNoTracking()
                join course in dbContext.ExternalCourses.AsNoTracking()
                    on subscription.ExternalCourseId equals course.Id
                where subscription.OwnerId == ownerId
                select new
                {
                    Subscription = subscription,
                    Course = course
                })
            .ToListAsync(cancellationToken);

        var courseIds = subscriptions
            .Select(item => item.Course.Id)
            .Distinct()
            .ToArray();
        var scanRuns = await dbContext.ScanRuns
            .AsNoTracking()
            .Where(scanRun => courseIds.Contains(scanRun.ExternalCourseId))
            .ToListAsync(cancellationToken);

        return subscriptions
            .OrderByDescending(item => item.Subscription.CreatedAtUtc)
            .Select(item => ToSubscriptionResult(
                item.Subscription,
                item.Course,
                scanRuns
                    .Where(scanRun => scanRun.ExternalCourseId == item.Course.Id)
                    .OrderByDescending(scanRun => scanRun.StartedAtUtc)
                    .FirstOrDefault()))
            .ToList();
    }

    public async Task<IReadOnlyList<ExternalContentResult>?> GetContentsAsync(
        Guid ownerId,
        Guid subscriptionId,
        CancellationToken cancellationToken = default)
    {
        var subscription = await dbContext.CourseSubscriptions
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.Id == subscriptionId && item.OwnerId == ownerId,
                cancellationToken);

        if (subscription is null)
        {
            return null;
        }

        var contents = await (
                from content in dbContext.ExternalContents.AsNoTracking()
                where content.ExternalCourseId == subscription.ExternalCourseId
                join link in dbContext.ExternalTaskLinks
                        .AsNoTracking()
                        .Where(item => item.CourseSubscriptionId == subscriptionId)
                    on content.Id equals link.ExternalContentId into contentLinks
                from link in contentLinks.DefaultIfEmpty()
                orderby content.Title, content.ProviderContentId
                select new
                {
                    Content = content,
                    TaskId = link == null ? (Guid?)null : link.TaskId
                })
            .ToListAsync(cancellationToken);

        return contents
            .Select(item => new ExternalContentResult(
                item.Content.Id,
                item.Content.ProviderContentId,
                item.Content.Title,
                item.Content.Description,
                item.Content.SourceUrl,
                item.Content.StructuredDueDateUtc,
                GetDisplayStatus(item.Content, item.TaskId),
                item.Content.ReviewReason == ExternalContentReviewReason.None
                    ? null
                    : item.Content.ReviewReason.ToString(),
                item.TaskId))
            .ToList();
    }

    internal static CourseSubscriptionResult ToSubscriptionResult(
        CourseSubscription subscription,
        ExternalCourse course,
        ScanRun? latestScanRun)
    {
        var lastScanStatus = course.ActiveScanRunId is not null
            ? ScanRunStatus.InProgress.ToString()
            : latestScanRun?.Status.ToString() ?? "NeverScanned";

        return new CourseSubscriptionResult(
            subscription.Id,
            subscription.ModuleId,
            course.Name,
            course.ProviderKey,
            course.ExternalCourseId,
            lastScanStatus,
            course.LastSuccessfulScanAtUtc);
    }

    private static ExternalContentDisplayStatus GetDisplayStatus(
        ExternalContent content,
        Guid? taskId)
    {
        if (content.Visibility == ExternalContentVisibility.NotVisible)
        {
            return ExternalContentDisplayStatus.NotVisible;
        }

        return taskId is not null
            ? ExternalContentDisplayStatus.TaskCreated
            : ExternalContentDisplayStatus.ReviewRequired;
    }
}
