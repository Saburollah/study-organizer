using StudyOrganizer.Application.ExternalCourses;
using StudyOrganizer.Domain.ExternalCourses;

namespace StudyOrganizer.Infrastructure.Tests.ExternalCourses;

public static class ExternalCourseSnapshots
{
    public static CourseSnapshot Initial { get; } = new(
        "mock-moodle",
        "software-engineering-2026",
        true,
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
        ]);

    public static CourseSnapshot Changed { get; } = new(
        "mock-moodle",
        "software-engineering-2026",
        true,
        [
            new CourseSnapshotItem(
                "exercise-1",
                ExternalContentKind.Assignment,
                "Exercise 1 revised",
                "Revised exercise details",
                new Uri("https://mock-moodle.local/content/exercise-1-v2"),
                new DateTimeOffset(2026, 9, 17, 12, 0, 0, TimeSpan.Zero)),
            new CourseSnapshotItem(
                "announcement-1",
                ExternalContentKind.Announcement,
                "Announcement 1",
                null,
                new Uri("https://mock-moodle.local/content/announcement-1"),
                null),
            new CourseSnapshotItem(
                "exercise-2",
                ExternalContentKind.Assignment,
                "Exercise 2",
                null,
                new Uri("https://mock-moodle.local/content/exercise-2"),
                new DateTimeOffset(2026, 9, 20, 12, 0, 0, TimeSpan.Zero))
        ]);

    public static CourseSnapshot WithoutExerciseOne { get; } = new(
        "mock-moodle",
        "software-engineering-2026",
        true,
        [Initial.Contents.Single(item => item.ProviderContentId == "announcement-1")]);

    public static CourseSnapshot WrongCourse { get; } = new(
        "mock-moodle",
        "other-course",
        true,
        Initial.Contents);

    public static CourseSnapshot DuplicateContentIds { get; } = new(
        "mock-moodle",
        "software-engineering-2026",
        true,
        [
            Initial.Contents[0],
            Initial.Contents[0] with { Title = "Duplicate exercise" }
        ]);
}
