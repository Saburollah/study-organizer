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
}
