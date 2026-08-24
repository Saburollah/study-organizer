using StudyOrganizer.Domain.ExternalCourses;

namespace StudyOrganizer.Domain.Tests.ExternalCourses;

public sealed class SubscriptionContentStateTests
{
    [Fact]
    public void Constructor_WithValidValues_CreatesImportedState()
    {
        // Arrange
        var courseSubscriptionId = Guid.NewGuid();
        var externalCourseId = Guid.NewGuid();
        var externalLearningContentId = Guid.NewGuid();
        var studyTaskId = Guid.NewGuid();
        var confirmedSignature = CreateSignature("Exercise sheet");
        var createdAt = DateTimeOffset.UnixEpoch;

        // Act
        var state = new SubscriptionContentState(
            courseSubscriptionId,
            externalCourseId,
            externalLearningContentId,
            studyTaskId,
            confirmedSignature,
            createdAt);

        // Assert
        Assert.NotEqual(Guid.Empty, state.Id);
        Assert.Equal(courseSubscriptionId, state.CourseSubscriptionId);
        Assert.Equal(externalCourseId, state.ExternalCourseId);
        Assert.Equal(
            externalLearningContentId,
            state.ExternalLearningContentId);
        Assert.Equal(
            SubscriptionContentStateStatus.Imported,
            state.Status);
        Assert.Equal(studyTaskId, state.StudyTaskId);
        Assert.Equal(confirmedSignature, state.ConfirmedSignature);
        Assert.Equal(createdAt, state.CreatedAt);
        Assert.Null(state.UpdatedAt);
    }

    [Theory]
    [InlineData(true, false, false, false, "courseSubscriptionId")]
    [InlineData(false, true, false, false, "externalCourseId")]
    [InlineData(false, false, true, false, "externalLearningContentId")]
    [InlineData(false, false, false, true, "studyTaskId")]
    public void Constructor_WithEmptyRequiredId_ThrowsArgumentException(
        bool emptyCourseSubscriptionId,
        bool emptyExternalCourseId,
        bool emptyExternalLearningContentId,
        bool emptyStudyTaskId,
        string expectedParameterName)
    {
        // Act
        var action = () => new SubscriptionContentState(
            emptyCourseSubscriptionId ? Guid.Empty : Guid.NewGuid(),
            emptyExternalCourseId ? Guid.Empty : Guid.NewGuid(),
            emptyExternalLearningContentId ? Guid.Empty : Guid.NewGuid(),
            emptyStudyTaskId ? Guid.Empty : Guid.NewGuid(),
            CreateSignature("Exercise sheet"),
            DateTimeOffset.UnixEpoch);

        // Assert
        var exception = Assert.Throws<ArgumentException>(action);
        Assert.Equal(expectedParameterName, exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullSignature_ThrowsArgumentNullException()
    {
        // Act
        var action = () => new SubscriptionContentState(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            null!,
            DateTimeOffset.UnixEpoch);

        // Assert
        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal("confirmedSignature", exception.ParamName);
    }

    [Fact]
    public void Dismiss_WhenImported_ClearsTaskAndIsIdempotent()
    {
        // Arrange
        var state = CreateImportedState();
        var dismissedAt = DateTimeOffset.UnixEpoch.AddHours(1);

        // Act
        state.Dismiss(dismissedAt);
        state.Dismiss(dismissedAt.AddHours(1));

        // Assert
        Assert.Equal(
            SubscriptionContentStateStatus.Dismissed,
            state.Status);
        Assert.Null(state.StudyTaskId);
        Assert.Equal(dismissedAt, state.UpdatedAt);
    }

    [Fact]
    public void Restore_WhenDismissed_CreatesNewImportedLink()
    {
        // Arrange
        var state = CreateImportedState();
        state.Dismiss(DateTimeOffset.UnixEpoch.AddHours(1));

        var newStudyTaskId = Guid.NewGuid();
        var currentSignature = CreateSignature("Renamed exercise sheet");
        var restoredAt = DateTimeOffset.UnixEpoch.AddHours(2);

        // Act
        state.Restore(
            newStudyTaskId,
            currentSignature,
            restoredAt);

        // Assert
        Assert.Equal(
            SubscriptionContentStateStatus.Imported,
            state.Status);
        Assert.Equal(newStudyTaskId, state.StudyTaskId);
        Assert.Equal(currentSignature, state.ConfirmedSignature);
        Assert.Equal(restoredAt, state.UpdatedAt);
    }

    [Fact]
    public void Restore_WithEmptyStudyTaskId_ThrowsArgumentException()
    {
        // Arrange
        var state = CreateImportedState();
        state.Dismiss(DateTimeOffset.UnixEpoch.AddHours(1));

        // Act
        var action = () => state.Restore(
            Guid.Empty,
            CreateSignature("Exercise sheet"),
            DateTimeOffset.UnixEpoch.AddHours(2));

        // Assert
        var exception = Assert.Throws<ArgumentException>(action);
        Assert.Equal("studyTaskId", exception.ParamName);
        Assert.Equal(
            SubscriptionContentStateStatus.Dismissed,
            state.Status);
    }

    [Fact]
    public void Restore_WithNullSignature_ThrowsArgumentNullException()
    {
        // Arrange
        var state = CreateImportedState();
        state.Dismiss(DateTimeOffset.UnixEpoch.AddHours(1));

        // Act
        var action = () => state.Restore(
            Guid.NewGuid(),
            null!,
            DateTimeOffset.UnixEpoch.AddHours(2));

        // Assert
        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal("currentSignature", exception.ParamName);
        Assert.Equal(
            SubscriptionContentStateStatus.Dismissed,
            state.Status);
    }

    [Fact]
    public void ConfirmSignature_WhenImported_UpdatesOnceAndIsIdempotent()
    {
        // Arrange
        var state = CreateImportedState();
        var newSignature = CreateSignature("Renamed exercise sheet");
        var confirmedAt = DateTimeOffset.UnixEpoch.AddHours(1);

        // Act
        state.ConfirmSignature(newSignature, confirmedAt);
        state.ConfirmSignature(newSignature, confirmedAt.AddHours(1));

        // Assert
        Assert.Equal(newSignature, state.ConfirmedSignature);
        Assert.Equal(confirmedAt, state.UpdatedAt);
        Assert.Equal(
            SubscriptionContentStateStatus.Imported,
            state.Status);
    }

    [Fact]
    public void Restore_WhenImported_ThrowsInvalidOperationException()
    {
        // Arrange
        var state = CreateImportedState();

        // Act
        var action = () => state.Restore(
            Guid.NewGuid(),
            CreateSignature("Exercise sheet"),
            DateTimeOffset.UnixEpoch.AddHours(1));

        // Assert
        Assert.Throws<InvalidOperationException>(action);
    }

    [Fact]
    public void ConfirmSignature_WhenDismissed_ThrowsInvalidOperationException()
    {
        // Arrange
        var state = CreateImportedState();
        state.Dismiss(DateTimeOffset.UnixEpoch.AddHours(1));

        // Act
        var action = () => state.ConfirmSignature(
            CreateSignature("Renamed exercise sheet"),
            DateTimeOffset.UnixEpoch.AddHours(2));

        // Assert
        Assert.Throws<InvalidOperationException>(action);
    }

    private static SubscriptionContentState CreateImportedState()
    {
        return new SubscriptionContentState(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            CreateSignature("Exercise sheet"),
            DateTimeOffset.UnixEpoch);
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
