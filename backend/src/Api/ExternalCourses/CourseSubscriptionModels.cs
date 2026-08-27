using System.ComponentModel.DataAnnotations;

namespace StudyOrganizer.Api.ExternalCourses;

public sealed class RegisterCourseSubscriptionRequest
{
    [Required]
    [StringLength(2048)]
    public string CourseUrl { get; init; } = string.Empty;
}

public sealed record CourseSubscriptionResponse(
    Guid ModuleId,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ActivatedAtUtc,
    ExternalCourseSummaryResponse Course,
    CourseSnapshotSummaryResponse? LatestSnapshot,
    ScanRunResponse? LatestScan,
    IReadOnlyList<ScanRunResponse> RecentScans);

public sealed record ExternalCourseSummaryResponse(
    string DisplayName,
    string SourceType,
    string? SourceUrl);

public sealed record CourseSnapshotSummaryResponse(
    DateTimeOffset ObservedAtUtc,
    int KnownContentCount);

public sealed record ScanRunResponse(
    Guid ScanRunId,
    string Status,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    ScanRunContentCountsResponse ContentCounts,
    ScanRunPersonalImpactResponse PersonalImpact,
    string? ErrorCode,
    bool CanRetry);

public sealed record ScanRunContentCountsResponse(
    int New,
    int Updated,
    int Unchanged,
    int Unavailable);

public sealed record ScanRunPersonalImpactResponse(
    int TasksCreated,
    int PdfTasksCreated,
    int NonPdfTasksCreated,
    int SourceUpdatesCreated);
