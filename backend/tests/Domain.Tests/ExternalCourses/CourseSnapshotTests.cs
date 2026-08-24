using StudyOrganizer.Domain.ExternalCourses;

namespace StudyOrganizer.Domain.Tests.ExternalCourses;

public sealed class CourseSnapshotTests
{
    [Fact]
    public void Constructor_WithValidValues_CreatesCurrentSnapshot()
    {
        // Arrange
        var externalCourseId = Guid.NewGuid();
        var scanRunId = Guid.NewGuid();

        var observedAt = new DateTimeOffset(
            2026,
            8,
            24,
            12,
            0,
            0,
            TimeSpan.Zero);

        // Act
        var snapshot = new CourseSnapshot(
            externalCourseId,
            scanRunId,
            observedAt);

        // Assert
        Assert.NotEqual(Guid.Empty, snapshot.Id);
        Assert.Equal(
            externalCourseId,
            snapshot.ExternalCourseId);
        Assert.Equal(scanRunId, snapshot.ScanRunId);
        Assert.Equal(observedAt, snapshot.ObservedAt);
        Assert.True(snapshot.IsCurrent);
    }
    [Theory]
    [InlineData(true, false, "externalCourseId")]
    [InlineData(false, true, "scanRunId")]
    public void Constructor_WithEmptyRequiredId_ThrowsArgumentException(
        bool emptyExternalCourseId,
        bool emptyScanRunId,
        string expectedParameterName)
    {
        // Arrange
        var externalCourseId = emptyExternalCourseId
            ? Guid.Empty
            : Guid.NewGuid();

        var scanRunId = emptyScanRunId
            ? Guid.Empty
            : Guid.NewGuid();

        // Act
        var action = () => new CourseSnapshot(
            externalCourseId,
            scanRunId,
            DateTimeOffset.UnixEpoch);

        // Assert
        var exception =
            Assert.Throws<ArgumentException>(action);

        Assert.Equal(
            expectedParameterName,
            exception.ParamName);
    }

    [Fact]
    public void MarkSuperseded_WhenCalledRepeatedly_RemainsHistorical()
    {
        // Arrange
        var snapshot = new CourseSnapshot(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UnixEpoch);

        // Act
        snapshot.MarkSuperseded();
        snapshot.MarkSuperseded();

        // Assert
        Assert.False(snapshot.IsCurrent);
    }
}
