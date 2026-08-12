namespace StudyOrganizer.Application.Modules;

public interface IModuleHandler
{
    Task<ModuleResult> CreateAsync(
        Guid ownerId,
        string name,
        string? code,
        string? description,
        string? color,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ModuleResult>> GetByOwnerAsync(
        Guid ownerId,
        CancellationToken cancellationToken = default);

    Task<ModuleResult?> UpdateAsync(
        Guid ownerId,
        Guid moduleId,
        string name,
        string? code,
        string? description,
        string? color,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid ownerId,
        Guid moduleId,
        CancellationToken cancellationToken = default);
}
