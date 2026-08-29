using Microsoft.EntityFrameworkCore;
using StudyOrganizer.Application.Tasks;
using StudyOrganizer.Domain.Tasks;
using StudyOrganizer.Infrastructure.Persistence;

namespace StudyOrganizer.Infrastructure.Tasks;

public sealed class StudyTaskHandler(
    ApplicationDbContext dbContext)
    : IStudyTaskHandler
{
    public async Task<StudyTaskResult?> CreateAsync(
        Guid ownerId,
        Guid moduleId,
        string title,
        DateTimeOffset dueDateUtc,
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

        return ToResult(task);
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

        var tasks = await (
                from task in dbContext.Tasks.AsNoTracking()
                where task.ModuleId == moduleId
                join link in dbContext.ExternalTaskLinks.AsNoTracking()
                    on task.Id equals link.TaskId into taskLinks
                from link in taskLinks.DefaultIfEmpty()
                join content in dbContext.ExternalContents.AsNoTracking()
                    on link.ExternalContentId equals content.Id into linkedContents
                from content in linkedContents.DefaultIfEmpty()
                join course in dbContext.ExternalCourses.AsNoTracking()
                    on content.ExternalCourseId equals course.Id into linkedCourses
                from course in linkedCourses.DefaultIfEmpty()
                select new
                {
                    Task = task,
                    ProviderKey = course == null ? null : course.ProviderKey,
                    CourseName = course == null ? null : course.Name,
                    SourceUrl = content == null ? null : content.SourceUrl
                })
            .ToListAsync(cancellationToken);

        return tasks
            .OrderBy(item => item.Task.DueDate)
            .ThenBy(item => item.Task.CreatedAt)
            .Select(item => ToResult(
                item.Task,
                item.ProviderKey is null
                    ? null
                    : new ExternalTaskSourceResult(
                        item.ProviderKey,
                        item.CourseName!,
                        item.SourceUrl!)))
            .ToList();
    }

    public async Task<StudyTaskMutationResult> UpdateAsync(
        Guid ownerId,
        Guid moduleId,
        Guid taskId,
        string title,
        DateTimeOffset dueDateUtc,
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
            return new StudyTaskMutationResult(
                StudyTaskMutationOutcome.NotFound,
                null);
        }

        if (await IsExternallyManagedAsync(taskId, cancellationToken))
        {
            return new StudyTaskMutationResult(
                StudyTaskMutationOutcome.ExternallyManaged,
                null);
        }

        task.Update(
            title,
            dueDateUtc,
            description);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        return new StudyTaskMutationResult(
            StudyTaskMutationOutcome.Succeeded,
            ToResult(task));
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

        return ToResult(task);
    }

    public async Task<StudyTaskMutationResult> DeleteAsync(
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
            return new StudyTaskMutationResult(
                StudyTaskMutationOutcome.NotFound,
                null);
        }

        if (await IsExternallyManagedAsync(taskId, cancellationToken))
        {
            return new StudyTaskMutationResult(
                StudyTaskMutationOutcome.ExternallyManaged,
                null);
        }

        dbContext.Tasks.Remove(task);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        return new StudyTaskMutationResult(
            StudyTaskMutationOutcome.Succeeded,
            null);
    }

    private Task<bool> IsExternallyManagedAsync(
        Guid taskId,
        CancellationToken cancellationToken) =>
        dbContext.ExternalTaskLinks.AnyAsync(
            link => link.TaskId == taskId,
            cancellationToken);

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

    private static StudyTaskResult ToResult(
        StudyTask task,
        ExternalTaskSourceResult? externalSource = null)
    {
        return new StudyTaskResult(
            task.Id,
            task.ModuleId,
            task.Title,
            task.Description,
            task.DueDate,
            task.Status,
            task.CreatedAt,
            task.UpdatedAt,
            externalSource);
    }
}
