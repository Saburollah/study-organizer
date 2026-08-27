using StudyOrganizer.Domain.ExternalCourses;

namespace StudyOrganizer.Application.ExternalCourses;

public enum CourseSubscriptionRegistrationOutcome
{
    Completed,
    Running,
    NotFound,
    UnsupportedCourseUrl,
    ModuleAlreadySubscribed,
    CourseAlreadySubscribed
}

public sealed record CourseSubscriptionRegistrationResult(
    CourseSubscriptionRegistrationOutcome Outcome,
    CourseSubscriptionResult? Subscription = null);

public enum CourseSubscriptionEndResult
{
    Ended,
    NotFound
}

public enum ScanRunRequestOutcome
{
    Completed,
    Running,
    NotFound
}

public sealed record ScanRunRequestResult(
    ScanRunRequestOutcome Outcome,
    ScanRunDetailsResult? Scan = null);

public sealed record CourseSubscriptionResult(
    Guid ModuleId,
    CourseSubscriptionState Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ActivatedAtUtc,
    ExternalCourseSummaryResult Course,
    CourseSnapshotSummaryResult? LatestSnapshot,
    ScanRunDetailsResult? LatestScan,
    IReadOnlyList<ScanRunDetailsResult> RecentScans);

public sealed record ExternalCourseSummaryResult(
    string DisplayName,
    string SourceType,
    string? SourceUrl);

public sealed record CourseSnapshotSummaryResult(
    DateTimeOffset ObservedAtUtc,
    int KnownContentCount);

public sealed record ScanRunDetailsResult(
    Guid ScanRunId,
    ScanRunStatus Status,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    ScanRunCounts ContentCounts,
    ScanRunPersonalImpactResult PersonalImpact,
    ScanRunErrorCode? ErrorCode,
    bool CanRetry);

public sealed record ScanRunPersonalImpactResult(
    int TasksCreated,
    int PdfTasksCreated,
    int NonPdfTasksCreated,
    int SourceUpdatesCreated);
