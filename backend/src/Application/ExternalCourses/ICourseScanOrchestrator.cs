namespace StudyOrganizer.Application.ExternalCourses;

public interface ICourseScanOrchestrator
{
    Task<ScanRunExecutionResult> ScanAsync(
        Guid externalCourseId,
        Guid? activationSubscriptionId = null,
        CancellationToken cancellationToken = default);
}
