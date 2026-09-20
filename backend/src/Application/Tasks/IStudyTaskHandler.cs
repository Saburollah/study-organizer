using StudyOrganizer.Domain.Tasks;

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

    Task<StudyTaskMutationResult> UpdateAsync(
        Guid ownerId,
        Guid moduleId,
        Guid taskId,
        string title,
        DateTimeOffset dueDateUtc,
        string? description,
        CancellationToken cancellationToken = default);

    Task<StudyTaskResult?> SetStatusAsync(
        Guid ownerId,
        Guid moduleId,
        Guid taskId,
        StudyTaskStatus status,
        CancellationToken cancellationToken = default);

    Task<StudyTaskMutationResult> DeleteAsync(
        Guid ownerId,
        Guid moduleId,
        Guid taskId,
        CancellationToken cancellationToken = default);
}
