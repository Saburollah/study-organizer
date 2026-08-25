using StudyOrganizer.Domain.Tasks;
using StudyOrganizer.Domain.ExternalCourses;

namespace StudyOrganizer.Application.Tasks;

public sealed record StudyTaskResult(
    Guid Id,
    Guid ModuleId,
    string Title,
    string? Description,
    DateTimeOffset? DueDateUtc,
    StudyTaskStatus Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc,
    StudyTaskImportSourceResult? ImportSource = null);

public enum StudyTaskImportSourceStatus
{
    Available,
    Unavailable,
    SubscriptionEnded,
    MetadataPurged
}

public sealed record StudyTaskImportSourceResult(
    StudyTaskImportSourceStatus Status,
    ExternalLearningContentType? ContentType,
    string? MediaType,
    string? SourceUrl,
    bool HasSourceUpdate);

public enum AcknowledgeSourceUpdateOutcome
{
    Succeeded,
    NotFound,
    TaskNotImported
}

public sealed record AcknowledgeSourceUpdateResult(
    AcknowledgeSourceUpdateOutcome Outcome,
    StudyTaskResult? Task = null);
