using StudyOrganizer.Domain.ExternalCourses;

namespace StudyOrganizer.Domain.Tests.ExternalCourses;

public sealed class ExternalContentTests
{
    [Fact]
    public void Create_WithCanonicalValues_NormalizesAndStartsVisible()
    {
        var dueDateUtc = new DateTimeOffset(2026, 9, 1, 12, 0, 0, TimeSpan.Zero);
        var lastSeenAtUtc = new DateTimeOffset(2026, 8, 28, 8, 0, 0, TimeSpan.Zero);

        var content = ExternalContent.Create(
            Guid.NewGuid(), " exercise-1 ", ExternalContentKind.Assignment,
            " Exercise 1 ", " Details ", " https://mock-moodle.local/content/exercise-1 ",
            dueDateUtc, ExternalContentProcessingState.TaskEligible,
            ExternalContentReviewReason.None, lastSeenAtUtc);

        Assert.Equal("exercise-1", content.ProviderContentId);
        Assert.Equal("Exercise 1", content.Title);
        Assert.Equal("Details", content.Description);
        Assert.Equal("https://mock-moodle.local/content/exercise-1", content.SourceUrl);
        Assert.Equal(ExternalContentVisibility.Visible, content.Visibility);
    }

    [Fact]
    public void Create_WithBlankRequiredString_Throws()
    {
        var exception = Assert.Throws<ArgumentException>(() => ExternalContent.Create(
            Guid.NewGuid(), " ", ExternalContentKind.Assignment,
            "Exercise 1", null, "https://mock-moodle.local/content/exercise-1",
            null, ExternalContentProcessingState.TaskEligible,
            ExternalContentReviewReason.None, DateTimeOffset.UtcNow));

        Assert.Equal("providerContentId", exception.ParamName);
    }

    [Fact]
    public void Create_WithEmptyExternalCourseId_Throws()
    {
        var exception = Assert.Throws<ArgumentException>(() => ExternalContent.Create(
            Guid.Empty, "exercise-1", ExternalContentKind.Assignment,
            "Exercise 1", null, "https://mock-moodle.local/content/exercise-1",
            null, ExternalContentProcessingState.TaskEligible,
            ExternalContentReviewReason.None, DateTimeOffset.UtcNow));

        Assert.Equal("externalCourseId", exception.ParamName);
    }

    [Fact]
    public void ApplySnapshot_PreservesIdentityAndUpdatesMutableFields()
    {
        var content = ExternalContent.Create(
            Guid.NewGuid(), "exercise-1", ExternalContentKind.Assignment,
            "Exercise 1", null, "https://mock-moodle.local/content/exercise-1",
            new DateTimeOffset(2026, 9, 1, 12, 0, 0, TimeSpan.Zero),
            ExternalContentProcessingState.TaskEligible,
            ExternalContentReviewReason.None,
            DateTimeOffset.UtcNow);
        var originalId = content.Id;

        content.ApplySnapshot(
            ExternalContentKind.Assignment, "Exercise 1 revised", "New text",
            "https://mock-moodle.local/content/exercise-1-v2",
            new DateTimeOffset(2026, 9, 2, 12, 0, 0, TimeSpan.Zero),
            ExternalContentProcessingState.TaskEligible,
            ExternalContentReviewReason.None,
            DateTimeOffset.UtcNow);

        Assert.Equal(originalId, content.Id);
        Assert.Equal("exercise-1", content.ProviderContentId);
        Assert.Equal("Exercise 1 revised", content.Title);
        Assert.Equal(ExternalContentVisibility.Visible, content.Visibility);
    }

    [Fact]
    public void MarkNotVisible_ChangesOnlyVisibility()
    {
        var content = CreateContent();
        var originalTitle = content.Title;
        var originalLastSeenAtUtc = content.LastSeenAtUtc;

        content.MarkNotVisible();

        Assert.Equal(ExternalContentVisibility.NotVisible, content.Visibility);
        Assert.Equal(originalTitle, content.Title);
        Assert.Equal(originalLastSeenAtUtc, content.LastSeenAtUtc);
    }

    [Fact]
    public void ExternalTaskLink_WithValidIdentity_CreatesLink()
    {
        var subscriptionId = Guid.NewGuid();
        var contentId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var createdAtUtc = new DateTimeOffset(2026, 8, 28, 8, 0, 0, TimeSpan.Zero);

        var link = new ExternalTaskLink(subscriptionId, contentId, taskId, createdAtUtc);

        Assert.NotEqual(Guid.Empty, link.Id);
        Assert.Equal(subscriptionId, link.CourseSubscriptionId);
        Assert.Equal(contentId, link.ExternalContentId);
        Assert.Equal(taskId, link.TaskId);
        Assert.Equal(createdAtUtc, link.CreatedAtUtc);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void ExternalTaskLink_WithEmptyRequiredId_Throws(int emptyIdPosition)
    {
        var subscriptionId = emptyIdPosition == 0 ? Guid.Empty : Guid.NewGuid();
        var contentId = emptyIdPosition == 1 ? Guid.Empty : Guid.NewGuid();
        var taskId = emptyIdPosition == 2 ? Guid.Empty : Guid.NewGuid();

        Assert.Throws<ArgumentException>(() => new ExternalTaskLink(
            subscriptionId,
            contentId,
            taskId,
            DateTimeOffset.UtcNow));
    }

    private static ExternalContent CreateContent()
    {
        return ExternalContent.Create(
            Guid.NewGuid(), "exercise-1", ExternalContentKind.Assignment,
            "Exercise 1", null, "https://mock-moodle.local/content/exercise-1",
            new DateTimeOffset(2026, 9, 1, 12, 0, 0, TimeSpan.Zero),
            ExternalContentProcessingState.TaskEligible,
            ExternalContentReviewReason.None,
            new DateTimeOffset(2026, 8, 28, 8, 0, 0, TimeSpan.Zero));
    }
}
