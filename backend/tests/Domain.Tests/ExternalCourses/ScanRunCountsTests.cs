using StudyOrganizer.Domain.ExternalCourses;

namespace StudyOrganizer.Domain.Tests.ExternalCourses;

public sealed class ScanRunCountsTests
{
    [Theory]
    [InlineData(-1, 0, 0, 0, "newCount")]
    [InlineData(0, -1, 0, 0, "updatedCount")]
    [InlineData(0, 0, -1, 0, "unchangedCount")]
    [InlineData(0, 0, 0, -1, "unavailableCount")]
    public void Constructor_WithNegativeCount_ThrowsArgumentOutOfRangeException(
        int newCount,
        int updatedCount,
        int unchangedCount,
        int unavailableCount,
        string expectedParameterName)
    {
        // Act
        var action = () => new ScanRunCounts(
            newCount,
            updatedCount,
            unchangedCount,
            unavailableCount);

        // Assert
        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(action);

        Assert.Equal(
            expectedParameterName,
            exception.ParamName);
    }
}
