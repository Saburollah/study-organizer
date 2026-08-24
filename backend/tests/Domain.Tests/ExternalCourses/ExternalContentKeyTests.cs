using StudyOrganizer.Domain.ExternalCourses;

namespace StudyOrganizer.Domain.Tests.ExternalCourses;

public sealed class ExternalContentKeyTests
{
    [Fact]
    public void Constructor_WithValidValue_CreatesExternalContentKey()
    {
        // Arrange
        const string value = "activity-17";

        // Act
        var externalContentKey =
            new ExternalContentKey(value);

        // Assert
        Assert.Equal(value, externalContentKey.Value);
    }

    [Fact]
    public void Constructor_WithSurroundingWhitespace_TrimsValue()
    {
        // Act
        var externalContentKey =
            new ExternalContentKey("  activity-17  ");

        // Assert
        Assert.Equal(
            "activity-17",
            externalContentKey.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithMissingValue_ThrowsArgumentException(
        string? value)
    {
        // Act
        var action =
            () => new ExternalContentKey(value!);

        // Assert
        var exception =
            Assert.Throws<ArgumentException>(action);

        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public void Equality_UsesExternalContentKeyValue()
    {
        // Arrange
        var externalContentKey =
            new ExternalContentKey("activity-17");

        // Assert
        Assert.Equal(
            externalContentKey,
            new ExternalContentKey("activity-17"));

        Assert.NotEqual(
            externalContentKey,
            new ExternalContentKey("activity-18"));

        Assert.NotEqual(
            externalContentKey,
            new ExternalContentKey("ACTIVITY-17"));
    }
}
