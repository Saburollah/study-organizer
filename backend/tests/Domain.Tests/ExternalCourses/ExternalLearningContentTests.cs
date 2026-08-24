using StudyOrganizer.Domain.ExternalCourses;

namespace StudyOrganizer.Domain.Tests.ExternalCourses;

public sealed class ExternalLearningContentTests
{
    [Fact]
    public void Constructor_WithValidValues_CreatesAvailableContent()
    {
        // Arrange
        var externalCourseId = Guid.NewGuid();
        var externalContentKey =
            new ExternalContentKey("file-17");

        var createdAt = new DateTimeOffset(
            2026,
            8,
            24,
            11,
            0,
            0,
            TimeSpan.Zero);

        var dueDate = createdAt.AddDays(7);

        // Act
        var content = new ExternalLearningContent(
            externalCourseId,
            externalContentKey,
            ExternalLearningContentType.File,
            "Exercise sheet 1",
            createdAt,
            dueDate,
            "application/pdf",
            "https://mock-moodle.test/file-17");

        // Assert
        Assert.NotEqual(Guid.Empty, content.Id);
        Assert.Equal(
            externalCourseId,
            content.ExternalCourseId);
        Assert.Equal(
            externalContentKey,
            content.ExternalContentKey);
        Assert.Equal(
            ExternalLearningContentType.File,
            content.Type);
        Assert.Equal("Exercise sheet 1", content.Title);
        Assert.Equal(dueDate, content.DueDate);
        Assert.Equal("application/pdf", content.MediaType);
        Assert.Equal(
            "https://mock-moodle.test/file-17",
            content.SourceReference);
        Assert.Equal(
            ExternalLearningContentAvailability.Available,
            content.Availability);
        Assert.Equal(createdAt, content.CreatedAt);
        Assert.Null(content.UpdatedAt);
    }

    [Fact]
    public void Constructor_WithTextValues_NormalizesMetadata()
    {
        // Act
        var content = new ExternalLearningContent(
            Guid.NewGuid(),
            new ExternalContentKey("link-17"),
            ExternalLearningContentType.Link,
            "  Course overview  ",
            DateTimeOffset.UnixEpoch,
            null,
            "  text/html  ",
            "  https://mock-moodle.test/link-17  ");

        // Assert
        Assert.Equal("Course overview", content.Title);
        Assert.Equal("text/html", content.MediaType);
        Assert.Equal(
            "https://mock-moodle.test/link-17",
            content.SourceReference);
    }

    [Fact]
    public void Constructor_WithEmptyExternalCourseId_ThrowsArgumentException()
    {
        // Act
        var action = () => new ExternalLearningContent(
            Guid.Empty,
            new ExternalContentKey("activity-17"),
            ExternalLearningContentType.Activity,
            "Forum discussion",
            DateTimeOffset.UnixEpoch,
            null,
            null,
            null);

        // Assert
        var exception =
            Assert.Throws<ArgumentException>(action);

        Assert.Equal(
            "externalCourseId",
            exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullExternalContentKey_ThrowsArgumentNullException()
    {
        // Act
        var action = () => new ExternalLearningContent(
            Guid.NewGuid(),
            null!,
            ExternalLearningContentType.Activity,
            "Forum discussion",
            DateTimeOffset.UnixEpoch,
            null,
            null,
            null);

        // Assert
        var exception =
            Assert.Throws<ArgumentNullException>(action);

        Assert.Equal(
            "externalContentKey",
            exception.ParamName);
    }

    [Fact]
    public void Constructor_WithEmptyTitle_ThrowsArgumentException()
    {
        // Act
        var action = () => new ExternalLearningContent(
            Guid.NewGuid(),
            new ExternalContentKey("activity-17"),
            ExternalLearningContentType.Activity,
            "   ",
            DateTimeOffset.UnixEpoch,
            null,
            null,
            null);

        // Assert
        var exception =
            Assert.Throws<ArgumentException>(action);

        Assert.Equal("title", exception.ParamName);
    }

    [Fact]
    public void UpdateMetadata_ChangesMetadataAndPreservesIdentity()
    {
        // Arrange
        var externalCourseId = Guid.NewGuid();
        var externalContentKey =
            new ExternalContentKey("content-17");

        var content = new ExternalLearningContent(
            externalCourseId,
            externalContentKey,
            ExternalLearningContentType.File,
            "Old title",
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch.AddDays(1),
            "application/pdf",
            "https://mock-moodle.test/old");

        var originalId = content.Id;
        var updatedAt =
            DateTimeOffset.UnixEpoch.AddHours(1);

        // Act
        content.UpdateMetadata(
            ExternalLearningContentType.Link,
            "  New title  ",
            null,
            "  text/html  ",
            "  https://mock-moodle.test/new  ",
            updatedAt);

        // Assert
        Assert.Equal(originalId, content.Id);
        Assert.Equal(
            externalCourseId,
            content.ExternalCourseId);
        Assert.Equal(
            externalContentKey,
            content.ExternalContentKey);
        Assert.Equal(
            ExternalLearningContentType.Link,
            content.Type);
        Assert.Equal("New title", content.Title);
        Assert.Null(content.DueDate);
        Assert.Equal("text/html", content.MediaType);
        Assert.Equal(
            "https://mock-moodle.test/new",
            content.SourceReference);
        Assert.Equal(updatedAt, content.UpdatedAt);
        Assert.Equal(
            ExternalLearningContentAvailability.Available,
            content.Availability);
    }

    [Fact]
    public void MarkUnavailable_WhenAvailable_MarksContentUnavailable()
    {
        // Arrange
        var content = new ExternalLearningContent(
            Guid.NewGuid(),
            new ExternalContentKey("activity-17"),
            ExternalLearningContentType.Activity,
            "Forum discussion",
            DateTimeOffset.UnixEpoch,
            null,
            null,
            null);

        var updatedAt =
            DateTimeOffset.UnixEpoch.AddHours(1);

        // Act
        content.MarkUnavailable(updatedAt);

        // Assert
        Assert.Equal(
            ExternalLearningContentAvailability.Unavailable,
            content.Availability);
        Assert.Equal(updatedAt, content.UpdatedAt);
    }

    [Fact]
    public void MarkUnavailable_WhenAlreadyUnavailable_PreservesUpdatedAt()
    {
        // Arrange
        var content = new ExternalLearningContent(
            Guid.NewGuid(),
            new ExternalContentKey("activity-17"),
            ExternalLearningContentType.Activity,
            "Forum discussion",
            DateTimeOffset.UnixEpoch,
            null,
            null,
            null);

        var firstUpdatedAt =
            DateTimeOffset.UnixEpoch.AddHours(1);

        content.MarkUnavailable(firstUpdatedAt);

        // Act
        content.MarkUnavailable(
            DateTimeOffset.UnixEpoch.AddHours(2));

        // Assert
        Assert.Equal(
            ExternalLearningContentAvailability.Unavailable,
            content.Availability);
        Assert.Equal(firstUpdatedAt, content.UpdatedAt);
    }

    [Fact]
    public void MarkAvailable_WhenUnavailable_MarksContentAvailable()
    {
        // Arrange
        var content = new ExternalLearningContent(
            Guid.NewGuid(),
            new ExternalContentKey("activity-17"),
            ExternalLearningContentType.Activity,
            "Forum discussion",
            DateTimeOffset.UnixEpoch,
            null,
            null,
            null);

        content.MarkUnavailable(
            DateTimeOffset.UnixEpoch.AddHours(1));

        var updatedAt =
            DateTimeOffset.UnixEpoch.AddHours(2);

        // Act
        content.MarkAvailable(updatedAt);

        // Assert
        Assert.Equal(
            ExternalLearningContentAvailability.Available,
            content.Availability);
        Assert.Equal(updatedAt, content.UpdatedAt);
    }

    [Fact]
    public void MarkAvailable_WhenAlreadyAvailable_PreservesUpdatedAt()
    {
        // Arrange
        var content = new ExternalLearningContent(
            Guid.NewGuid(),
            new ExternalContentKey("activity-17"),
            ExternalLearningContentType.Activity,
            "Forum discussion",
            DateTimeOffset.UnixEpoch,
            null,
            null,
            null);

        content.MarkUnavailable(
            DateTimeOffset.UnixEpoch.AddHours(1));

        var firstAvailableAt =
            DateTimeOffset.UnixEpoch.AddHours(2);

        content.MarkAvailable(firstAvailableAt);

        // Act
        content.MarkAvailable(
            DateTimeOffset.UnixEpoch.AddHours(3));

        // Assert
        Assert.Equal(
            ExternalLearningContentAvailability.Available,
            content.Availability);
        Assert.Equal(firstAvailableAt, content.UpdatedAt);
    }
}
