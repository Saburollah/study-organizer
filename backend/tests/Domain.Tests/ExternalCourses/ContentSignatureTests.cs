using StudyOrganizer.Domain.ExternalCourses;

namespace StudyOrganizer.Domain.Tests.ExternalCourses;

public sealed class ContentSignatureTests
{
    [Fact]
    public void Compute_WithSameValues_ReturnsSameVersionedSignature()
    {
        // Arrange
        var dueDate =
            DateTimeOffset.UnixEpoch.AddDays(1);

        // Act
        var first = ContentSignature.Compute(
            ExternalLearningContentType.File,
            "Lecture notes",
            dueDate,
            "application/pdf",
            "/mod/resource/17",
            ExternalLearningContentAvailability.Available);

        var second = ContentSignature.Compute(
            ExternalLearningContentType.File,
            "Lecture notes",
            dueDate,
            "application/pdf",
            "/mod/resource/17",
            ExternalLearningContentAvailability.Available);

        // Assert
        Assert.Equal(1, first.Version);
        Assert.Matches("^[0-9a-f]{64}$", first.Hash);
        Assert.Equal(first, second);
    }

    [Fact]
    public void Compute_WithEquivalentNormalizedValues_ReturnsSameSignature()
    {
        // Arrange
        var dueDateWithOffset = new DateTimeOffset(
            2026,
            8,
            24,
            12,
            0,
            0,
            TimeSpan.FromHours(2));

        // Act
        var first = ContentSignature.Compute(
            ExternalLearningContentType.File,
            " Lecture notes ",
            dueDateWithOffset,
            " application/pdf ",
            " /mod/resource/17 ",
            ExternalLearningContentAvailability.Available);

        var second = ContentSignature.Compute(
            ExternalLearningContentType.File,
            "Lecture notes",
            dueDateWithOffset.ToUniversalTime(),
            "application/pdf",
            "/mod/resource/17",
            ExternalLearningContentAvailability.Available);

        // Assert
        Assert.Equal(first, second);
    }
}
