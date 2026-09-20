using StudyOrganizer.Application.ExternalCourses;
using StudyOrganizer.Domain.ExternalCourses;

namespace StudyOrganizer.Infrastructure.ExternalCourses;

public sealed class MockMoodleProvider : IExternalCourseProvider
{
    public const string Key = "mock-moodle";
    public const string CourseId = "software-engineering-2026";

    private static readonly HashSet<string> SupportedUrls = new(
        StringComparer.Ordinal)
    {
        "https://mock-moodle.local/courses/software-engineering-2026",
        "https://mock-moodle.local/course/view.php?id=se-2026"
    };

    public string ProviderKey => Key;

    public bool CanHandle(Uri courseUri)
    {
        return courseUri.IsAbsoluteUri
            && SupportedUrls.Contains(courseUri.AbsoluteUri);
    }

    public Task<CourseDiscovery> DiscoverAsync(
        Uri courseUri,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!CanHandle(courseUri))
        {
            throw new ExternalCourseProviderException(
                ExternalCourseProviderError.UnsupportedUrl);
        }

        return Task.FromResult(
            new CourseDiscovery(
                Key,
                CourseId,
                "Software Engineering"));
    }

    public Task<CourseSnapshot> FetchSnapshotAsync(
        string externalCourseId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!string.Equals(externalCourseId, CourseId, StringComparison.Ordinal))
        {
            throw new ExternalCourseProviderException(
                ExternalCourseProviderError.InvalidResponse);
        }

        IReadOnlyList<CourseSnapshotItem> contents =
        [
            new CourseSnapshotItem(
                "exercise-1",
                ExternalContentKind.Assignment,
                "Exercise 1",
                null,
                new Uri("https://mock-moodle.local/content/exercise-1"),
                new DateTimeOffset(2026, 9, 15, 12, 0, 0, TimeSpan.Zero)),
            new CourseSnapshotItem(
                "announcement-1",
                ExternalContentKind.Announcement,
                "Announcement 1",
                null,
                new Uri("https://mock-moodle.local/content/announcement-1"),
                null)
        ];

        return Task.FromResult(
            new CourseSnapshot(
                Key,
                CourseId,
                true,
                contents));
    }
}
