using StudyOrganizer.Domain.ExternalCourses;

namespace StudyOrganizer.Domain.Tests.ExternalCourses;

public sealed class ExternalCourseIdentityTests
{
    [Fact]
    public void Constructor_WithValidValues_CreatesIdentity()
    {
        // Arrange
        const string sourceType = "mock-moodle";
        const string sourceInstance = "campus-a";
        const string externalCourseKey = "course-42";

        // Act
        var identity = new ExternalCourseIdentity(
            sourceType,
            sourceInstance,
            externalCourseKey);

        // Assert
        Assert.Equal(sourceType, identity.SourceType);
        Assert.Equal(sourceInstance, identity.SourceInstance);
        Assert.Equal(
            externalCourseKey,
            identity.ExternalCourseKey);
    }

    [Fact]
    public void Constructor_WithSurroundingWhitespace_TrimsValues()
    {
        // Act
        var identity = new ExternalCourseIdentity(
            "  mock-moodle  ",
            "  campus-a  ",
            "  course-42  ");

        // Assert
        Assert.Equal("mock-moodle", identity.SourceType);
        Assert.Equal("campus-a", identity.SourceInstance);
        Assert.Equal(
            "course-42",
            identity.ExternalCourseKey);
    }

    [Theory]
    [InlineData(null, "campus-a", "course-42", "sourceType")]
    [InlineData("mock-moodle", "   ", "course-42", "sourceInstance")]
    [InlineData("mock-moodle", "campus-a", "", "externalCourseKey")]
    public void Constructor_WithMissingValue_ThrowsArgumentException(
        string? sourceType,
        string? sourceInstance,
        string? externalCourseKey,
        string expectedParameterName)
    {
        // Act
        var action = () => new ExternalCourseIdentity(
            sourceType!,
            sourceInstance!,
            externalCourseKey!);

        // Assert
        var exception =
            Assert.Throws<ArgumentException>(action);

        Assert.Equal(
            expectedParameterName,
            exception.ParamName);
    }

    [Fact]
    public void Equality_UsesAllIdentityComponents()
    {
        // Arrange
        var identity = new ExternalCourseIdentity(
            "mock-moodle",
            "campus-a",
            "course-42");

        // Assert
        Assert.Equal(
            identity,
            new ExternalCourseIdentity(
                "mock-moodle",
                "campus-a",
                "course-42"));

        Assert.NotEqual(
            identity,
            new ExternalCourseIdentity(
                "other-source",
                "campus-a",
                "course-42"));

        Assert.NotEqual(
            identity,
            new ExternalCourseIdentity(
                "mock-moodle",
                "campus-b",
                "course-42"));

        Assert.NotEqual(
            identity,
            new ExternalCourseIdentity(
                "mock-moodle",
                "campus-a",
                "course-99"));
    }
}
