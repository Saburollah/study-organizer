using StudyOrganizer.Domain.ExternalCourses;

namespace StudyOrganizer.Domain.Tests.ExternalCourses;

public sealed class CourseSnapshotItemTests
{
    [Fact]
    public void Constructor_WithValidValues_CopiesObservedMetadata()
    {
        // Arrange
        var courseSnapshotId = Guid.NewGuid();
        var externalCourseId = Guid.NewGuid();
        var externalLearningContentId = Guid.NewGuid();
        var externalContentKey =
            new ExternalContentKey("file-17");

        var dueDate =
            DateTimeOffset.UnixEpoch.AddDays(7);

        // Act
        var item = new CourseSnapshotItem(
            courseSnapshotId,
            externalCourseId,
            externalLearningContentId,
            externalContentKey,
            ExternalLearningContentType.File,
            "Exercise sheet 1",
            dueDate,
            "application/pdf",
            "/mod/resource/17");

        // Assert
        Assert.Equal(
            courseSnapshotId,
            item.CourseSnapshotId);
        Assert.Equal(
            externalCourseId,
            item.ExternalCourseId);
        Assert.Equal(
            externalLearningContentId,
            item.ExternalLearningContentId);
        Assert.Equal(
            externalContentKey,
            item.ExternalContentKey);
        Assert.Equal(
            ExternalLearningContentType.File,
            item.Type);
        Assert.Equal("Exercise sheet 1", item.Title);
        Assert.Equal(dueDate, item.DueDate);
        Assert.Equal("application/pdf", item.MediaType);
        Assert.Equal(
            "/mod/resource/17",
            item.SourceReference);
        Assert.Equal(
            ContentSignature.Compute(
                ExternalLearningContentType.File,
                "Exercise sheet 1",
                dueDate,
                "application/pdf",
                "/mod/resource/17",
                ExternalLearningContentAvailability.Available),
            item.Signature);
    }
    [Fact]
    public void Constructor_WithTextValues_NormalizesMetadata()
    {
        // Act
        var item = new CourseSnapshotItem(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            new ExternalContentKey("link-17"),
            ExternalLearningContentType.Link,
            "  Course overview  ",
            null,
            "   ",
            "  /course/view/17  ");

        // Assert
        Assert.Equal("Course overview", item.Title);
        Assert.Null(item.MediaType);
        Assert.Equal(
            "/course/view/17",
            item.SourceReference);
        Assert.Equal(
            ContentSignature.Compute(
                ExternalLearningContentType.Link,
                "Course overview",
                null,
                null,
                "/course/view/17",
                ExternalLearningContentAvailability.Available),
            item.Signature);
    }

    [Theory]
    [InlineData(true, false, false, "courseSnapshotId")]
    [InlineData(false, true, false, "externalCourseId")]
    [InlineData(
        false,
        false,
        true,
        "externalLearningContentId")]
    public void Constructor_WithEmptyRequiredId_ThrowsArgumentException(
        bool emptyCourseSnapshotId,
        bool emptyExternalCourseId,
        bool emptyExternalLearningContentId,
        string expectedParameterName)
    {
        // Act
        var action = () => new CourseSnapshotItem(
            emptyCourseSnapshotId
                ? Guid.Empty
                : Guid.NewGuid(),
            emptyExternalCourseId
                ? Guid.Empty
                : Guid.NewGuid(),
            emptyExternalLearningContentId
                ? Guid.Empty
                : Guid.NewGuid(),
            new ExternalContentKey("file-17"),
            ExternalLearningContentType.File,
            "Exercise sheet",
            null,
            null,
            null);

        // Assert
        var exception =
            Assert.Throws<ArgumentException>(action);

        Assert.Equal(
            expectedParameterName,
            exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullExternalContentKey_ThrowsArgumentNullException()
    {
        // Act
        var action = () => new CourseSnapshotItem(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            null!,
            ExternalLearningContentType.File,
            "Exercise sheet",
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

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_WithEmptyTitle_ThrowsArgumentException(
        string? title)
    {
        // Act
        var action = () => new CourseSnapshotItem(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            new ExternalContentKey("file-17"),
            ExternalLearningContentType.File,
            title!,
            null,
            null,
            null);

        // Assert
        var exception =
            Assert.Throws<ArgumentException>(action);

        Assert.Equal("title", exception.ParamName);
    }
}
