using StudyOrganizer.Domain.Tasks;

namespace StudyOrganizer.Application.Tasks;

public sealed record StudyTaskResult(
    Guid Id,
    Guid ModuleId,
    string Title,
    string? Description,
    DateTimeOffset DueDateUtc,
    StudyTaskStatus Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);
