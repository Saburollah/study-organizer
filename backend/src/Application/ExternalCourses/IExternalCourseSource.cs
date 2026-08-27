using StudyOrganizer.Domain.ExternalCourses;

namespace StudyOrganizer.Application.ExternalCourses;

public interface IExternalCourseSource
{
    Task<ExternalCourseSourcePayload> FetchCourseDataAsync(
        ExternalCourseIdentity identity,
        CancellationToken cancellationToken = default);
}
