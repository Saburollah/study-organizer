using StudyOrganizer.Domain.ExternalCourses;

namespace StudyOrganizer.Application.ExternalCourses;

public interface IExternalCourseUrlResolver
{
    ResolvedExternalCourse? Resolve(string courseUrl);

    string? GetSafeContentUrl(
        ExternalCourseIdentity identity,
        string? sourceReference);
}

public sealed record ResolvedExternalCourse(
    ExternalCourseIdentity Identity,
    string DisplayName,
    string SourceUrl);
