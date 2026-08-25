using StudyOrganizer.Domain.ExternalCourses;

namespace StudyOrganizer.Application.ExternalCourses;

public interface IExternalCourseSource
{
    Task<CourseSourceSnapshot> FetchSnapshotAsync(
        ExternalCourseIdentity identity,
        CancellationToken cancellationToken = default);
}
