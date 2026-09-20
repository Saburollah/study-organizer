namespace StudyOrganizer.Application.ExternalCourses;

public interface IExternalCourseQueryHandler
{
    Task<IReadOnlyList<CourseSubscriptionResult>> GetByOwnerAsync(
        Guid ownerId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExternalContentResult>?> GetContentsAsync(
        Guid ownerId,
        Guid subscriptionId,
        CancellationToken cancellationToken = default);
}
