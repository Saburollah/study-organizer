using StudyOrganizer.Domain.ExternalCourses;

namespace StudyOrganizer.Domain.Tests.ExternalCourses;

public sealed class CourseSubscriptionTests
{
    [Fact]
    public void Constructor_WithValidValues_CreatesPendingCourseSubscription()
    {
        // Arrange
        var studyModuleId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var externalCourseId = Guid.NewGuid();

        var createdAt = new DateTimeOffset(
            2026,
            8,
            24,
            10,
            0,
            0,
            TimeSpan.Zero);

        // Act
        var courseSubscription = new CourseSubscription(
            studyModuleId,
            ownerId,
            externalCourseId,
            createdAt);

        // Assert
        Assert.NotEqual(Guid.Empty, courseSubscription.Id);
        Assert.Equal(
            studyModuleId,
            courseSubscription.StudyModuleId);
        Assert.Equal(ownerId, courseSubscription.OwnerId);
        Assert.Equal(
            externalCourseId,
            courseSubscription.ExternalCourseId);
        Assert.Equal(
            CourseSubscriptionState.Pending,
            courseSubscription.State);
        Assert.Equal(
            createdAt,
            courseSubscription.CreatedAt);
        Assert.Null(courseSubscription.ActivatedAt);
        Assert.Null(courseSubscription.EndedAt);
    }

    [Theory]
    [InlineData("studyModuleId")]
    [InlineData("ownerId")]
    [InlineData("externalCourseId")]
    public void Constructor_WithEmptyId_ThrowsArgumentException(
        string expectedParameterName)
    {
        // Arrange
        var studyModuleId =
            expectedParameterName == "studyModuleId"
                ? Guid.Empty
                : Guid.NewGuid();

        var ownerId =
            expectedParameterName == "ownerId"
                ? Guid.Empty
                : Guid.NewGuid();

        var externalCourseId =
            expectedParameterName == "externalCourseId"
                ? Guid.Empty
                : Guid.NewGuid();

        // Act
        var action = () => new CourseSubscription(
            studyModuleId,
            ownerId,
            externalCourseId,
            DateTimeOffset.UnixEpoch);

        // Assert
        var exception =
            Assert.Throws<ArgumentException>(action);

        Assert.Equal(
            expectedParameterName,
            exception.ParamName);
    }

    [Fact]
    public void Activate_WhenPending_MarksCourseSubscriptionActive()
    {
        // Arrange
        var courseSubscription = new CourseSubscription(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UnixEpoch);

        var activatedAt =
            DateTimeOffset.UnixEpoch.AddHours(1);

        // Act
        courseSubscription.Activate(activatedAt);

        // Assert
        Assert.Equal(
            CourseSubscriptionState.Active,
            courseSubscription.State);
        Assert.Equal(
            activatedAt,
            courseSubscription.ActivatedAt);
        Assert.Null(courseSubscription.EndedAt);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void End_WhenNotEnded_MarksCourseSubscriptionEnded(
        bool activateFirst)
    {
        // Arrange
        var courseSubscription = new CourseSubscription(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UnixEpoch);

        if (activateFirst)
        {
            courseSubscription.Activate(
                DateTimeOffset.UnixEpoch.AddHours(1));
        }

        var endedAt =
            DateTimeOffset.UnixEpoch.AddHours(2);

        // Act
        courseSubscription.End(endedAt);

        // Assert
        Assert.Equal(
            CourseSubscriptionState.Ended,
            courseSubscription.State);
        Assert.Equal(
            endedAt,
            courseSubscription.EndedAt);
    }

    [Fact]
    public void End_WhenAlreadyEnded_PreservesEndedAt()
    {
        // Arrange
        var courseSubscription = new CourseSubscription(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UnixEpoch);

        var firstEndedAt =
            DateTimeOffset.UnixEpoch.AddHours(1);

        courseSubscription.End(firstEndedAt);

        // Act
        courseSubscription.End(
            DateTimeOffset.UnixEpoch.AddHours(2));

        // Assert
        Assert.Equal(
            firstEndedAt,
            courseSubscription.EndedAt);
    }

    [Fact]
    public void BeginReactivation_WhenEnded_MarksCourseSubscriptionPending()
    {
        // Arrange
        var courseSubscription = new CourseSubscription(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UnixEpoch);

        courseSubscription.Activate(
            DateTimeOffset.UnixEpoch.AddHours(1));

        courseSubscription.End(
            DateTimeOffset.UnixEpoch.AddHours(2));

        // Act
        courseSubscription.BeginReactivation();

        // Assert
        Assert.Equal(
            CourseSubscriptionState.Pending,
            courseSubscription.State);
        Assert.Null(courseSubscription.ActivatedAt);
        Assert.Null(courseSubscription.EndedAt);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BeginReactivation_WhenNotEnded_ThrowsInvalidOperationException(
        bool activateFirst)
    {
        // Arrange
        var courseSubscription = new CourseSubscription(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UnixEpoch);

        if (activateFirst)
        {
            courseSubscription.Activate(
                DateTimeOffset.UnixEpoch.AddHours(1));
        }

        // Act
        var action =
            courseSubscription.BeginReactivation;

        // Assert
        Assert.Throws<InvalidOperationException>(action);
    }

    [Theory]
    [InlineData(CourseSubscriptionState.Active)]
    [InlineData(CourseSubscriptionState.Ended)]
    public void Activate_WhenNotPending_ThrowsInvalidOperationException(
        CourseSubscriptionState state)
    {
        // Arrange
        var courseSubscription = new CourseSubscription(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UnixEpoch);

        if (state == CourseSubscriptionState.Active)
        {
            courseSubscription.Activate(
                DateTimeOffset.UnixEpoch.AddHours(1));
        }
        else
        {
            courseSubscription.End(
                DateTimeOffset.UnixEpoch.AddHours(1));
        }

        // Act
        var action = () => courseSubscription.Activate(
            DateTimeOffset.UnixEpoch.AddHours(2));

        // Assert
        Assert.Throws<InvalidOperationException>(action);
    }
}
