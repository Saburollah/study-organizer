namespace StudyOrganizer.Application.Tasks;

public interface IStudyTaskHandler
{
    Task<StudyTaskResult?> CreateAsync(
        Guid ownerId,
        Guid moduleId,
        string title,
        DateTimeOffset dueDateUtc,
        string? description,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StudyTaskResult>?>
        GetByModuleAsync(
            Guid ownerId,
            Guid moduleId,
            CancellationToken cancellationToken = default);
}
