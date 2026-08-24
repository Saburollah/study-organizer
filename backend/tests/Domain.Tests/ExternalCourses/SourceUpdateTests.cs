using StudyOrganizer.Domain.ExternalCourses;

namespace StudyOrganizer.Domain.Tests.ExternalCourses;

public sealed class SourceUpdateTests
{
    [Fact]
    public void Constructor_WithValidValues_CreatesOpenUpdate()
    {
        // Arrange
        var subscriptionContentStateId = Guid.NewGuid();
        var detectedByScanRunId = Guid.NewGuid();
        var detectedSignature = CreateSignature("Renamed exercise sheet");
        var detectedAt = DateTimeOffset.UnixEpoch.AddHours(1);

        // Act
        var update = new SourceUpdate(
            subscriptionContentStateId,
            detectedSignature,
            detectedAt,
            detectedByScanRunId);

        // Assert
        Assert.NotEqual(Guid.Empty, update.Id);
        Assert.Equal(
            subscriptionContentStateId,
            update.SubscriptionContentStateId);
        Assert.Equal(detectedSignature, update.DetectedSignature);
        Assert.Equal(detectedAt, update.DetectedAt);
        Assert.Equal(detectedByScanRunId, update.DetectedByScanRunId);
    }

    [Theory]
    [InlineData(true, false, "subscriptionContentStateId")]
    [InlineData(false, true, "detectedByScanRunId")]
    public void Constructor_WithEmptyId_ThrowsArgumentException(
        bool emptyStateId,
        bool emptyScanRunId,
        string expectedParameterName)
    {
        // Act
        var action = () => new SourceUpdate(
            emptyStateId ? Guid.Empty : Guid.NewGuid(),
            CreateSignature("Renamed exercise sheet"),
            DateTimeOffset.UnixEpoch,
            emptyScanRunId ? Guid.Empty : Guid.NewGuid());

        // Assert
        var exception = Assert.Throws<ArgumentException>(action);
        Assert.Equal(expectedParameterName, exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullSignature_ThrowsArgumentNullException()
    {
        // Act
        var action = () => new SourceUpdate(
            Guid.NewGuid(),
            null!,
            DateTimeOffset.UnixEpoch);

        // Assert
        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal("detectedSignature", exception.ParamName);
    }

    [Fact]
    public void Refresh_WithNewSignature_UpdatesOnceAndIsIdempotent()
    {
        // Arrange
        var update = new SourceUpdate(
            Guid.NewGuid(),
            CreateSignature("Old title"),
            DateTimeOffset.UnixEpoch,
            Guid.NewGuid());

        var newSignature = CreateSignature("New title");
        var detectedAt = DateTimeOffset.UnixEpoch.AddHours(1);
        var detectedByScanRunId = Guid.NewGuid();

        // Act
        update.Refresh(
            newSignature,
            detectedAt,
            detectedByScanRunId);
        update.Refresh(
            newSignature,
            detectedAt.AddHours(1),
            Guid.NewGuid());

        // Assert
        Assert.Equal(newSignature, update.DetectedSignature);
        Assert.Equal(detectedAt, update.DetectedAt);
        Assert.Equal(detectedByScanRunId, update.DetectedByScanRunId);
    }

    private static ContentSignature CreateSignature(string title)
    {
        return ContentSignature.Compute(
            ExternalLearningContentType.File,
            title,
            null,
            "application/pdf",
            "/mod/resource/17",
            ExternalLearningContentAvailability.Available);
    }
}
