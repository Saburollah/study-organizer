using StudyOrganizer.Domain.ExternalCourses;

namespace StudyOrganizer.Domain.Tests.ExternalCourses;

public sealed class ExternalCourseTests
{
    [Fact]
    public void Constructor_WithValidValues_CreatesInactiveExternalCourse()
    {
        // Arrange
        var identity = new ExternalCourseIdentity(
            "mock-moodle",
            "campus-a",
            "course-42");

        var createdAt = new DateTimeOffset(
            2026,
            8,
            24,
            9,
            30,
            0,
            TimeSpan.Zero);

        // Act
        var externalCourse = new ExternalCourse(
            identity,
            "Distributed Systems",
            createdAt);

        // Assert
        Assert.NotEqual(Guid.Empty, externalCourse.Id);
        Assert.Equal(identity, externalCourse.Identity);
        Assert.Equal(
            "Distributed Systems",
            externalCourse.Name);
        Assert.Equal(
            ExternalCourseState.Inactive,
            externalCourse.State);
        Assert.Equal(createdAt, externalCourse.CreatedAt);
        Assert.Equal(
            createdAt,
            externalCourse.InactiveSince);
    }

    [Fact]
    public void Constructor_WithSurroundingWhitespace_TrimsName()
    {
        // Arrange
        var identity = new ExternalCourseIdentity(
            "mock-moodle",
            "campus-a",
            "course-42");

        // Act
        var externalCourse = new ExternalCourse(
            identity,
            "  Distributed Systems  ",
            DateTimeOffset.UnixEpoch);

        // Assert
        Assert.Equal(
            "Distributed Systems",
            externalCourse.Name);
    }

    [Fact]
    public void Constructor_WithNullIdentity_ThrowsArgumentNullException()
    {
        // Act
        var action = () => new ExternalCourse(
            null!,
            "Distributed Systems",
            DateTimeOffset.UnixEpoch);

        // Assert
        var exception =
            Assert.Throws<ArgumentNullException>(action);

        Assert.Equal("identity", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithEmptyName_ThrowsArgumentException()
    {
        // Arrange
        var identity = new ExternalCourseIdentity(
            "mock-moodle",
            "campus-a",
            "course-42");

        // Act
        var action = () => new ExternalCourse(
            identity,
            "   ",
            DateTimeOffset.UnixEpoch);

        // Assert
        var exception =
            Assert.Throws<ArgumentException>(action);

        Assert.Equal("name", exception.ParamName);
    }

    [Fact]
    public void Activate_WhenInactive_MarksExternalCourseActive()
    {
        // Arrange
        var externalCourse = new ExternalCourse(
            new ExternalCourseIdentity(
                "mock-moodle",
                "campus-a",
                "course-42"),
            "Distributed Systems",
            DateTimeOffset.UnixEpoch);

        // Act
        externalCourse.Activate();

        // Assert
        Assert.Equal(
            ExternalCourseState.Active,
            externalCourse.State);
        Assert.Null(externalCourse.InactiveSince);
    }

    [Fact]
    public void Deactivate_WhenActive_MarksExternalCourseInactive()
    {
        // Arrange
        var externalCourse = new ExternalCourse(
            new ExternalCourseIdentity(
                "mock-moodle",
                "campus-a",
                "course-42"),
            "Distributed Systems",
            DateTimeOffset.UnixEpoch);

        externalCourse.Activate();

        var inactiveAt =
            DateTimeOffset.UnixEpoch.AddDays(1);

        // Act
        externalCourse.Deactivate(inactiveAt);

        // Assert
        Assert.Equal(
            ExternalCourseState.Inactive,
            externalCourse.State);
        Assert.Equal(
            inactiveAt,
            externalCourse.InactiveSince);
    }

    [Fact]
    public void Deactivate_WhenAlreadyInactive_PreservesInactiveSince()
    {
        // Arrange
        var externalCourse = new ExternalCourse(
            new ExternalCourseIdentity(
                "mock-moodle",
                "campus-a",
                "course-42"),
            "Distributed Systems",
            DateTimeOffset.UnixEpoch);

        externalCourse.Activate();

        var firstInactiveAt =
            DateTimeOffset.UnixEpoch.AddDays(1);

        externalCourse.Deactivate(firstInactiveAt);

        // Act
        externalCourse.Deactivate(
            DateTimeOffset.UnixEpoch.AddDays(2));

        // Assert
        Assert.Equal(
            firstInactiveAt,
            externalCourse.InactiveSince);
    }
}
