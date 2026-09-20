namespace StudyOrganizer.Application.ExternalCourses;

public enum CourseRegistrationOutcome
{
    Created,
    Existing,
    InvalidUrl,
    UnsupportedUrl
}

public sealed record CourseSubscriptionResult(
    Guid Id,
    Guid ModuleId,
    string CourseName,
    string ProviderKey,
    string ExternalCourseId,
    string LastScanStatus,
    DateTimeOffset? LastSuccessfulScanAtUtc);

public sealed record CourseRegistrationResult(
    CourseRegistrationOutcome Outcome,
    CourseSubscriptionResult? Subscription);

public enum ExternalContentDisplayStatus
{
    TaskCreated,
    ReviewRequired,
    NotVisible
}

public sealed record ExternalContentResult(
    Guid Id,
    string ProviderContentId,
    string Title,
    string? Description,
    string SourceUrl,
    DateTimeOffset? DueDateUtc,
    ExternalContentDisplayStatus Status,
    string? ReviewReason,
    Guid? TaskId);

public enum CourseScanOutcome
{
    Succeeded,
    NotFound,
    AlreadyRunning,
    ExternalFailure,
    InvalidSnapshot
}

public sealed record CourseScanSummary(
    int NewContentCount,
    int ChangedContentCount,
    int ReviewRequiredCount,
    int NotVisibleCount,
    int NewTaskEligibleCount);

public sealed record CourseScanResult(
    CourseScanOutcome Outcome,
    CourseScanSummary? Summary,
    string? ErrorCode);
