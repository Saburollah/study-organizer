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
