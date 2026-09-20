using StudyOrganizer.Application.ExternalCourses;
using StudyOrganizer.Domain.ExternalCourses;
using StudyOrganizer.Infrastructure.ExternalCourses;

namespace StudyOrganizer.Infrastructure.Tests.ExternalCourses;

public sealed class MockMoodleProviderTests
{
    [Theory]
    [InlineData("https://mock-moodle.local/courses/software-engineering-2026")]
    [InlineData("https://mock-moodle.local/course/view.php?id=se-2026")]
    public void CanHandle_ExactFixtureAlias_ReturnsTrue(string courseUrl)
    {
        var provider = new MockMoodleProvider();

        Assert.True(provider.CanHandle(new Uri(courseUrl)));
    }

    [Theory]
    [InlineData("http://mock-moodle.local/courses/software-engineering-2026")]
    [InlineData("https://mock-moodle.local/courses/software-engineering-2026/")]
    [InlineData("https://mock-moodle.local/courses/software-engineering-2026?x=1")]
    [InlineData("https://other.local/courses/software-engineering-2026")]
    [InlineData("https://mock-moodle.local/course/view.php?id=other")]
    public void CanHandle_NonFixtureUrl_ReturnsFalse(string courseUrl)
    {
        var provider = new MockMoodleProvider();

        Assert.False(provider.CanHandle(new Uri(courseUrl)));
    }

    [Fact]
    public async Task DiscoverAsync_FixtureAlias_ReturnsCanonicalCourse()
    {
        var provider = new MockMoodleProvider();

        var discovery = await provider.DiscoverAsync(
            new Uri("https://mock-moodle.local/course/view.php?id=se-2026"));

        Assert.Equal("mock-moodle", discovery.ProviderKey);
        Assert.Equal("software-engineering-2026", discovery.ExternalCourseId);
        Assert.Equal("Software Engineering", discovery.Name);
    }

    [Fact]
    public async Task DiscoverAsync_UnsupportedUrl_ThrowsSafeProviderError()
    {
        var provider = new MockMoodleProvider();

        var exception = await Assert.ThrowsAsync<ExternalCourseProviderException>(() =>
            provider.DiscoverAsync(new Uri("https://mock-moodle.local/courses/other")));

        Assert.Equal(ExternalCourseProviderError.UnsupportedUrl, exception.Error);
    }

    [Fact]
    public async Task FetchSnapshotAsync_CanonicalCourse_ReturnsFixedNetworkFreeSnapshot()
    {
        var provider = new MockMoodleProvider();

        var snapshot = await provider.FetchSnapshotAsync("software-engineering-2026");

        Assert.Equal("mock-moodle", snapshot.ProviderKey);
        Assert.Equal("software-engineering-2026", snapshot.ExternalCourseId);
        Assert.True(snapshot.IsComplete);
        Assert.Collection(
            snapshot.Contents,
            exercise =>
            {
                Assert.Equal("exercise-1", exercise.ProviderContentId);
                Assert.Equal(ExternalContentKind.Assignment, exercise.Kind);
                Assert.Equal("Exercise 1", exercise.Title);
                Assert.Equal(
                    "https://mock-moodle.local/content/exercise-1",
                    exercise.SourceUri.AbsoluteUri);
                Assert.Equal(
                    new DateTimeOffset(2026, 9, 15, 12, 0, 0, TimeSpan.Zero),
                    exercise.StructuredDueDateUtc);
            },
            announcement =>
            {
                Assert.Equal("announcement-1", announcement.ProviderContentId);
                Assert.Equal(ExternalContentKind.Announcement, announcement.Kind);
                Assert.Equal("Announcement 1", announcement.Title);
                Assert.Equal(
                    "https://mock-moodle.local/content/announcement-1",
                    announcement.SourceUri.AbsoluteUri);
                Assert.Null(announcement.StructuredDueDateUtc);
            });
    }
}
