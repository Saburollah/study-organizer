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
        return await dbContext.Modules
            .AsNoTracking()
            .Where(module =>
                module.OwnerId == ownerId)
            .OrderByDescending(module =>
                module.CreatedAt)
            .Select(module =>
                new ModuleResult(
                    module.Id,
                    module.Name,
                    module.Code,
                    module.Description,
                    module.Color,
                    module.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    private static ModuleResult ToResult(
        StudyModule module)
    {
        return new ModuleResult(
            module.Id,
            module.Name,
            module.Code,
            module.Description,
            module.Color,
            module.CreatedAt);
    }
}
