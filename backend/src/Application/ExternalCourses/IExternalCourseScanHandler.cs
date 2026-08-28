namespace StudyOrganizer.Application.ExternalCourses;

public interface IExternalCourseScanHandler
{
    Task<CourseScanResult> ScanAsync(
        Guid ownerId,
        Guid subscriptionId,
        CancellationToken cancellationToken = default);
}
