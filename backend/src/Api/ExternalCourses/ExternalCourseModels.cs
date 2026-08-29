using System.ComponentModel.DataAnnotations;

namespace StudyOrganizer.Api.ExternalCourses;

public sealed class RegisterCourseSubscriptionRequest
{
    [Required]
    [StringLength(2048)]
    [Url]
    public string CourseUrl { get; init; } = string.Empty;
}

public sealed record CourseSubscriptionResponse(
    Guid Id,
    Guid ModuleId,
    string CourseName,
    string ProviderKey,
    string ExternalCourseId,
    string LastScanStatus,
    DateTimeOffset? LastSuccessfulScanAtUtc);

public sealed record ExternalCourseContentResponse(
    Guid Id,
    string ProviderContentId,
    string Title,
    string? Description,
    string SourceUrl,
    DateTimeOffset? DueDateUtc,
    string Status,
    string? ReviewReason,
    Guid? TaskId);

public sealed record CourseScanResponse(
    string Status,
    int NewContentCount,
    int ChangedContentCount,
    int ReviewRequiredCount,
    int NotVisibleCount,
    int NewTaskEligibleCount);
