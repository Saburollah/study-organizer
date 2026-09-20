namespace StudyOrganizer.Application.ExternalCourses;

public interface IExternalCourseRegistrationHandler
{
    Task<CourseRegistrationResult> RegisterAsync(
        Guid ownerId,
        string courseUrl,
        CancellationToken cancellationToken = default);
}
