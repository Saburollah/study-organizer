using System.ComponentModel.DataAnnotations;

namespace StudyOrganizer.Api.Tasks;

public sealed class CreateStudyTaskRequest
{
    [Required]
    [StringLength(200)]
    public string Title { get; init; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; init; }

    public DateTimeOffset? DueDateUtc { get; init; }
}

public sealed class UpdateStudyTaskRequest
{
    [Required]
    [StringLength(200)]
    public string Title { get; init; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; init; }

    public DateTimeOffset? DueDateUtc { get; init; }
}

public sealed class UpdateStudyTaskStatusRequest
{
    [Required]
    public string Status { get; init; } = string.Empty;
}

public sealed record StudyTaskResponse(
    Guid Id,
    Guid ModuleId,
    string Title,
    string? Description,
    DateTimeOffset? DueDateUtc,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc,
    StudyTaskImportSourceResponse? ImportSource);

public sealed record StudyTaskImportSourceResponse(
    string Status,
    string? ContentType,
    string? MediaType,
    string? SourceUrl,
    bool HasSourceUpdate);
