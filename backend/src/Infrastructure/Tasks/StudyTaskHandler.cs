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

        return await dbContext.Tasks
            .AsNoTracking()
            .Where(task =>
                task.ModuleId == moduleId)
            .OrderBy(task =>
                task.DueDate)
            .ThenBy(task =>
                task.CreatedAt)
            .Select(task =>
                new StudyTaskResult(
                    task.Id,
                    task.ModuleId,
                    task.Title,
                    task.Description,
                    task.DueDate,
                    task.Status,
                    task.CreatedAt,
                    task.UpdatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<StudyTaskResult?> UpdateAsync(
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
            return null;
        }

        task.Update(
            title,
            dueDateUtc,
            description);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        return ToResult(task);
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

        dbContext.Tasks.Remove(task);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        return true;
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

    private static StudyTaskResult ToResult(
        StudyTask task)
    {
        return new StudyTaskResult(
            task.Id,
            task.ModuleId,
            task.Title,
            task.Description,
            task.DueDate,
            task.Status,
            task.CreatedAt,
            task.UpdatedAt);
    }
}
