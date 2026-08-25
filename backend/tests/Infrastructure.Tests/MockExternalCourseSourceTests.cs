using StudyOrganizer.Application.ExternalCourses;
using StudyOrganizer.Domain.ExternalCourses;
using StudyOrganizer.Infrastructure.ExternalCourses;

namespace StudyOrganizer.Infrastructure.Tests;

public sealed class MockExternalCourseSourceTests
{
    [Fact]
    public async Task FetchSnapshotAsync_ForSupportedMockIdentity_ReturnsDefaultCourse()
    {
        var identity = new ExternalCourseIdentity(
            MockMoodleCourseUrlResolver.SourceType,
            MockMoodleCourseUrlResolver.SourceInstance,
            "software-engineering");
        var source = new MockExternalCourseSource();

        var snapshot = await source.FetchSnapshotAsync(identity);

        Assert.Equal(3, snapshot.Items.Count);
        Assert.Single(snapshot.Items.Where(item =>
            item.Type == ExternalLearningContentType.File
            && item.MediaType == "application/pdf"));
        Assert.Equal(1, source.GetFetchCount(identity));
    }

    [Fact]
    public async Task FetchSnapshotAsync_ForDefaultCourse_AdvancesOnceAndThenStaysStable()
    {
        var identity = new ExternalCourseIdentity(
            MockMoodleCourseUrlResolver.SourceType,
            MockMoodleCourseUrlResolver.SourceInstance,
            "software-engineering");
        var source = new MockExternalCourseSource();

        var initial = await source.FetchSnapshotAsync(identity);
        var updated = await source.FetchSnapshotAsync(identity);
        var repeated = await source.FetchSnapshotAsync(identity);

        Assert.Equal(3, initial.Items.Count);
        Assert.Equal(4, updated.Items.Count);
        Assert.Equal(
            ["reading-pdf", "reference-link", "practice-activity", "project-brief"],
            updated.Items.Select(item => item.ExternalContentKey.Value));
        Assert.Equal(
            updated.Items.Select(item => item.ExternalContentKey.Value),
            repeated.Items.Select(item => item.ExternalContentKey.Value));
        Assert.Equal(3, source.GetFetchCount(identity));
    }

    [Fact]
    public async Task FetchSnapshotAsync_AfterVersionChange_ReturnsSelectedVersion()
    {
        // Arrange
        var identity = new ExternalCourseIdentity(
            "mock-moodle",
            "https://moodle.example.test",
            "course-17");
        var source = new MockExternalCourseSource();
        source.RegisterCourse(
            identity,
            "initial",
            new Dictionary<string, CourseSourceSnapshot>
            {
                ["initial"] = CreateSnapshot("Initial exercise"),
                ["updated"] = CreateSnapshot("Updated exercise")
            });

        var initial = await source.FetchSnapshotAsync(identity);
        source.UseVersion(identity, "updated");

        // Act
        var updated = await source.FetchSnapshotAsync(identity);

        // Assert
        Assert.Equal("Initial exercise", initial.Items.Single().Title);
        Assert.Equal("Updated exercise", updated.Items.Single().Title);
        Assert.Equal(2, source.GetFetchCount(identity));
    }

    [Fact]
    public async Task FetchSnapshotAsync_WithConfiguredFailure_ThrowsStableError()
    {
        // Arrange
        var identity = new ExternalCourseIdentity(
            "mock-moodle",
            "https://moodle.example.test",
            "course-17");
        var source = new MockExternalCourseSource();
        source.RegisterCourse(
            identity,
            "initial",
            new Dictionary<string, CourseSourceSnapshot>
            {
                ["initial"] = CreateSnapshot("Initial exercise")
            });
        source.FailWith(identity, ScanRunErrorCode.AccessDenied);

        // Act
        var exception = await Assert.ThrowsAsync<
            ExternalCourseSourceException>(() =>
                source.FetchSnapshotAsync(identity));

        // Assert
        Assert.Equal(ScanRunErrorCode.AccessDenied, exception.ErrorCode);
        Assert.Equal(1, source.GetFetchCount(identity));

        source.ClearFailure(identity);
        var snapshot = await source.FetchSnapshotAsync(identity);
        Assert.Equal("Initial exercise", snapshot.Items.Single().Title);
    }

    private static CourseSourceSnapshot CreateSnapshot(string title)
    {
        return new CourseSourceSnapshot(
        [
            new CourseSourceItem(
                new ExternalContentKey("file-17"),
                ExternalLearningContentType.File,
                title,
                null,
                "application/pdf",
                "/mod/resource/17")
        ]);
    }
}
