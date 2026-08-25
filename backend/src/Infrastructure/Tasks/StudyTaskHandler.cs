using Microsoft.EntityFrameworkCore;
using StudyOrganizer.Application.ExternalCourses;
using StudyOrganizer.Application.Tasks;
using StudyOrganizer.Domain.ExternalCourses;
using StudyOrganizer.Domain.Tasks;
using StudyOrganizer.Infrastructure.Persistence;

namespace StudyOrganizer.Infrastructure.Tasks;

public sealed class StudyTaskHandler(
    ApplicationDbContext dbContext,
    IExternalCourseUrlResolver courseUrlResolver,
    TimeProvider timeProvider)
    : IStudyTaskHandler
{
    public async Task<StudyTaskResult?> CreateAsync(
        Guid ownerId,
        Guid moduleId,
        string title,
        DateTimeOffset? dueDateUtc,
        string? description,
        CancellationToken cancellationToken = default)
    {
        var ownsModule =
            await dbContext.Modules.AnyAsync(
                module =>
                    module.Id == moduleId
                    && module.OwnerId == ownerId,
                cancellationToken);

        if (!ownsModule)
        {
            return null;
        }

        var task = new StudyTask(
            moduleId,
            title,
            dueDateUtc,
            description);

        dbContext.Tasks.Add(task);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        return await ToResultAsync(task, cancellationToken);
    }

    public async Task<IReadOnlyList<StudyTaskResult>?>
        GetByModuleAsync(
            Guid ownerId,
            Guid moduleId,
            CancellationToken cancellationToken = default)
    {
        var ownsModule =
            await dbContext.Modules.AnyAsync(
                module =>
                    module.Id == moduleId
                    && module.OwnerId == ownerId,
                cancellationToken);

        if (!ownsModule)
        {
            return null;
        }

        var tasks = await dbContext.Tasks
            .AsNoTracking()
            .Where(task =>
                task.ModuleId == moduleId)
            .OrderBy(task =>
                task.DueDate)
            .ThenBy(task =>
                task.CreatedAt)
            .ToListAsync(cancellationToken);

        var results = new List<StudyTaskResult>(tasks.Count);
        foreach (var task in tasks)
        {
            results.Add(
                await ToResultAsync(task, cancellationToken));
        }

        return results;
    }

    public async Task<StudyTaskResult?> UpdateAsync(
        Guid ownerId,
        Guid moduleId,
        Guid taskId,
        string title,
        DateTimeOffset? dueDateUtc,
        string? description,
        CancellationToken cancellationToken = default)
    {
        var task = await GetOwnedTaskAsync(
            ownerId,
            moduleId,
            taskId,
            cancellationToken);

        if (task is null)
        {
            return null;
        }

        task.Update(
            title,
            dueDateUtc,
            description);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        return await ToResultAsync(task, cancellationToken);
    }

    public async Task<StudyTaskResult?> SetStatusAsync(
        Guid ownerId,
        Guid moduleId,
        Guid taskId,
        StudyTaskStatus status,
        CancellationToken cancellationToken = default)
    {
        var task = await GetOwnedTaskAsync(
            ownerId,
            moduleId,
            taskId,
            cancellationToken);

        if (task is null)
        {
            return null;
        }

        switch (status)
        {
            case StudyTaskStatus.Open:
                task.Reopen();
                break;
            case StudyTaskStatus.Completed:
                task.Complete();
                break;
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(status),
                    status,
                    "Unsupported task status.");
        }

        await dbContext.SaveChangesAsync(
            cancellationToken);

        return await ToResultAsync(task, cancellationToken);
    }

    public async Task<bool> DeleteAsync(
        Guid ownerId,
        Guid moduleId,
        Guid taskId,
        CancellationToken cancellationToken = default)
    {
        var task = await GetOwnedTaskAsync(
            ownerId,
            moduleId,
            taskId,
            cancellationToken);

        if (task is null)
        {
            return false;
        }

        var importState = await dbContext.SubscriptionContentStates
            .SingleOrDefaultAsync(
                state =>
                    state.StudyTaskId == taskId
                    && state.Status ==
                        SubscriptionContentStateStatus.Imported,
                cancellationToken);

        if (importState is null)
        {
            dbContext.Tasks.Remove(task);
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }

        await using var transaction =
            await dbContext.Database.BeginTransactionAsync(
                cancellationToken);
        var sourceUpdate = await dbContext.SourceUpdates
            .SingleOrDefaultAsync(
                update =>
                    update.SubscriptionContentStateId == importState.Id,
                cancellationToken);
        if (sourceUpdate is not null)
        {
            dbContext.SourceUpdates.Remove(sourceUpdate);
        }

        importState.Dismiss(timeProvider.GetUtcNow());
        await dbContext.SaveChangesAsync(cancellationToken);

        dbContext.Tasks.Remove(task);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return true;
    }

    public async Task<AcknowledgeSourceUpdateResult>
        AcknowledgeSourceUpdateAsync(
            Guid ownerId,
            Guid moduleId,
            Guid taskId,
            CancellationToken cancellationToken = default)
    {
        var task = await GetOwnedTaskAsync(
            ownerId,
            moduleId,
            taskId,
            cancellationToken);
        if (task is null)
        {
            return new AcknowledgeSourceUpdateResult(
                AcknowledgeSourceUpdateOutcome.NotFound);
        }

        var importState = await dbContext.SubscriptionContentStates
            .SingleOrDefaultAsync(
                state =>
                    state.StudyTaskId == taskId
                    && state.Status ==
                        SubscriptionContentStateStatus.Imported,
                cancellationToken);
        if (importState is null)
        {
            return new AcknowledgeSourceUpdateResult(
                AcknowledgeSourceUpdateOutcome.TaskNotImported);
        }

        var sourceUpdate = await dbContext.SourceUpdates
            .SingleOrDefaultAsync(
                update =>
                    update.SubscriptionContentStateId == importState.Id,
                cancellationToken);
        if (sourceUpdate is not null)
        {
            importState.ConfirmSignature(
                sourceUpdate.DetectedSignature,
                timeProvider.GetUtcNow());
            dbContext.SourceUpdates.Remove(sourceUpdate);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return new AcknowledgeSourceUpdateResult(
            AcknowledgeSourceUpdateOutcome.Succeeded,
            await ToResultAsync(task, cancellationToken));
    }

    private Task<StudyTask?> GetOwnedTaskAsync(
        Guid ownerId,
        Guid moduleId,
        Guid taskId,
        CancellationToken cancellationToken)
    {
        return dbContext.Tasks.SingleOrDefaultAsync(
            task =>
                task.Id == taskId
                && task.ModuleId == moduleId
                && dbContext.Modules.Any(module =>
                    module.Id == moduleId
                    && module.OwnerId == ownerId),
            cancellationToken);
    }

    private async Task<StudyTaskResult> ToResultAsync(
        StudyTask task,
        CancellationToken cancellationToken)
    {
        var importSource = await BuildImportSourceAsync(
            task.Id,
            cancellationToken);

        return new StudyTaskResult(
            task.Id,
            task.ModuleId,
            task.Title,
            task.Description,
            task.DueDate,
            task.Status,
            task.CreatedAt,
            task.UpdatedAt,
            importSource);
    }

    private async Task<StudyTaskImportSourceResult?>
        BuildImportSourceAsync(
            Guid taskId,
            CancellationToken cancellationToken)
    {
        var source = await (
            from state in dbContext.SubscriptionContentStates.AsNoTracking()
            join subscription in dbContext.CourseSubscriptions.AsNoTracking()
                on state.CourseSubscriptionId equals subscription.Id
            join content in dbContext.ExternalLearningContents.AsNoTracking()
                on state.ExternalLearningContentId equals content.Id
            join course in dbContext.ExternalCourses.AsNoTracking()
                on state.ExternalCourseId equals course.Id
            where state.StudyTaskId == taskId
                && state.Status ==
                    SubscriptionContentStateStatus.Imported
            select new
            {
                Subscription = subscription,
                Content = content,
                Course = course
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (source is null)
        {
            return null;
        }

        var hasSourceUpdate = await (
            from update in dbContext.SourceUpdates.AsNoTracking()
            join state in dbContext.SubscriptionContentStates.AsNoTracking()
                on update.SubscriptionContentStateId equals state.Id
            where state.StudyTaskId == taskId
            select update.Id)
            .AnyAsync(cancellationToken);

        var hasCourseAccess = source.Subscription.State ==
            CourseSubscriptionState.Active;
        var status = hasCourseAccess
            ? source.Content.Availability ==
                ExternalLearningContentAvailability.Available
                ? StudyTaskImportSourceStatus.Available
                : StudyTaskImportSourceStatus.Unavailable
            : StudyTaskImportSourceStatus.SubscriptionEnded;

        return new StudyTaskImportSourceResult(
            status,
            hasCourseAccess ? source.Content.Type : null,
            hasCourseAccess ? source.Content.MediaType : null,
            hasCourseAccess
                ? courseUrlResolver.GetSafeContentUrl(
                    source.Course.Identity,
                    source.Content.SourceReference)
                : null,
            hasSourceUpdate);
    }
}
