using Microsoft.EntityFrameworkCore;
using StudyOrganizer.Application.Modules;
using StudyOrganizer.Domain.Modules;
using StudyOrganizer.Infrastructure.Persistence;

namespace StudyOrganizer.Infrastructure.Modules;

public sealed class ModuleHandler(
    ApplicationDbContext dbContext)
    : IModuleHandler
{
    public async Task<ModuleResult> CreateAsync(
        Guid ownerId,
        string name,
        string? code,
        string? description,
        string? color,
        CancellationToken cancellationToken = default)
    {
        var module = new StudyModule(
            ownerId,
            name,
            code,
            description,
            color);

        dbContext.Modules.Add(module);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        return ToResult(module);
    }

    public async Task<IReadOnlyList<ModuleResult>>
        GetByOwnerAsync(
            Guid ownerId,
            CancellationToken cancellationToken = default)
    {
        var modules = await dbContext.Modules
            .AsNoTracking()
            .Where(module =>
                module.OwnerId == ownerId)
            .Select(module => new ModuleResult(
                module.Id,
                module.Name,
                module.Code,
                module.Description,
                module.Color,
                module.CreatedAt,
                dbContext.CourseSubscriptions.Any(
                    subscription => subscription.ModuleId == module.Id)))
            .ToListAsync(cancellationToken);

        return modules
            .OrderByDescending(module => module.CreatedAt)
            .ToList();
    }
    public async Task<ModuleResult?> UpdateAsync(
        Guid ownerId,
        Guid moduleId,
        string name,
        string? code,
        string? description,
        string? color,
        CancellationToken cancellationToken = default)
    {
        var module = await dbContext.Modules
            .SingleOrDefaultAsync(
                item =>
                    item.Id == moduleId
                    && item.OwnerId == ownerId,
                cancellationToken);

        if (module is null)
        {
            return null;
        }

        module.Update(
            name,
            code,
            description,
            color);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        var isExternalCourseLinked = await dbContext.CourseSubscriptions.AnyAsync(
            subscription => subscription.ModuleId == module.Id,
            cancellationToken);

        return ToResult(module, isExternalCourseLinked);
    }

    public async Task<ModuleDeleteOutcome> DeleteAsync(
        Guid ownerId,
        Guid moduleId,
        CancellationToken cancellationToken = default)
    {
        var module = await dbContext.Modules
            .SingleOrDefaultAsync(
                item =>
                    item.Id == moduleId
                    && item.OwnerId == ownerId,
                cancellationToken);

        if (module is null)
        {
            return ModuleDeleteOutcome.NotFound;
        }

        if (await dbContext.CourseSubscriptions.AnyAsync(
                subscription => subscription.ModuleId == moduleId,
                cancellationToken))
        {
            return ModuleDeleteOutcome.LinkedToExternalCourse;
        }

        dbContext.Modules.Remove(module);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        return ModuleDeleteOutcome.Deleted;
    }

    private static ModuleResult ToResult(
        StudyModule module,
        bool isExternalCourseLinked = false)
    {
        return new ModuleResult(
            module.Id,
            module.Name,
            module.Code,
            module.Description,
            module.Color,
            module.CreatedAt,
            isExternalCourseLinked);
    }
}
