namespace StudyOrganizer.Application.ExternalCourses;

public interface ICourseScanOrchestrator
{
    Task<CourseScanResult> ScanAsync(
        Guid externalCourseId,
        Guid? activationSubscriptionId = null,
        CancellationToken cancellationToken = default);
}
