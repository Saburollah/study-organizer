namespace StudyOrganizer.Application.ExternalCourses;

public interface ICourseSubscriptionHandler
{
    Task<CourseSubscriptionRegistrationResult> RegisterAsync(
        Guid ownerId,
        Guid moduleId,
        string courseUrl,
        CancellationToken cancellationToken = default);

    Task<CourseSubscriptionResult?> GetAsync(
        Guid ownerId,
        Guid moduleId,
        CancellationToken cancellationToken = default);

    Task<CourseSubscriptionEndResult> EndAsync(
        Guid ownerId,
        Guid moduleId,
        CancellationToken cancellationToken = default);

    Task<CourseScanRequestResult> StartScanAsync(
        Guid ownerId,
        Guid moduleId,
        CancellationToken cancellationToken = default);

    Task<CourseScanResultDetails?> GetScanAsync(
        Guid ownerId,
        Guid moduleId,
        Guid scanRunId,
        CancellationToken cancellationToken = default);
}
