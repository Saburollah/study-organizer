using StudyOrganizer.Application.ExternalCourses;
using StudyOrganizer.Domain.ExternalCourses;

namespace StudyOrganizer.Application.Tests.ExternalCourses;

public sealed class CourseSnapshotDifferTests
{
    [Fact]
    public void Compare_NewStableId_ReturnsNew()
    {
        var incoming = CreateSnapshot(CreateIncoming("exercise-1"));

        var diff = CourseSnapshotDiffer.Compare([], incoming);

        var change = Assert.Single(diff.Changes);
        Assert.Equal(CourseContentChangeKind.New, change.Kind);
        Assert.Null(change.Existing);
        Assert.Equal("exercise-1", change.Incoming!.ProviderContentId);
    }

    [Fact]
    public void Compare_SameValues_ReturnsUnchanged()
    {
        var existing = CreateExisting();
        var incoming = CreateSnapshot(CreateIncoming("exercise-1"));

        var diff = CourseSnapshotDiffer.Compare([existing], incoming);

        var change = Assert.Single(diff.Changes);
        Assert.Equal(CourseContentChangeKind.Unchanged, change.Kind);
        Assert.Same(existing, change.Existing);
        Assert.Equal("exercise-1", change.Incoming!.ProviderContentId);
    }

    [Fact]
    public void Compare_SameIdWithChangedTitleLinkAndDeadline_ReturnsChanged()
    {
        var existing = CreateExisting();
        var incoming = CreateSnapshot(new CourseSnapshotItem(
            "exercise-1",
            ExternalContentKind.Resource,
            "Exercise 1 revised",
            "New description",
            new Uri("https://mock-moodle.local/content/exercise-1-v2"),
            new DateTimeOffset(2026, 9, 2, 12, 0, 0, TimeSpan.Zero)));

        var diff = CourseSnapshotDiffer.Compare([existing], incoming);

        var change = Assert.Single(diff.Changes);
        Assert.Equal(CourseContentChangeKind.Changed, change.Kind);
        Assert.Same(existing, change.Existing);
        Assert.Equal("exercise-1", change.Incoming!.ProviderContentId);
        Assert.Equal(ExternalContentKind.Resource, change.Incoming.Kind);
    }

    [Fact]
    public void Compare_MissingStableId_ReturnsMissing()
    {
        var existing = CreateExisting();
        var incoming = CreateSnapshot();

        var diff = CourseSnapshotDiffer.Compare([existing], incoming);

        var change = Assert.Single(diff.Changes);
        Assert.Equal(CourseContentChangeKind.Missing, change.Kind);
        Assert.Same(existing, change.Existing);
        Assert.Null(change.Incoming);
    }

    [Fact]
    public void Compare_DuplicateIncomingIds_ThrowsInvalidSnapshot()
    {
        var incoming = CreateSnapshot(
            CreateIncoming("exercise-1"),
            CreateIncoming("exercise-1"));

        Assert.Throws<InvalidCourseSnapshotException>(() =>
            CourseSnapshotDiffer.Compare([], incoming));
    }

    [Fact]
    public void Compare_Changes_ReturnsProviderContentIdOrder()
    {
        var existing = new[]
        {
            CreateExisting("z-item"),
            CreateExisting("m-item")
        };
        var incoming = CreateSnapshot(
            CreateIncoming("a-item"),
            CreateIncoming("m-item"));

        var diff = CourseSnapshotDiffer.Compare(existing, incoming);

        Assert.Collection(
            diff.Changes,
            change => Assert.Equal("a-item", change.Incoming!.ProviderContentId),
            change => Assert.Equal("m-item", change.Incoming!.ProviderContentId),
            change => Assert.Equal("z-item", change.Existing!.ProviderContentId));
    }

    private static CourseSnapshot CreateSnapshot(params CourseSnapshotItem[] contents)
    {
        return new CourseSnapshot("moodle", "course-1", false, contents);
    }

    private static ExistingContentState CreateExisting(string providerContentId = "exercise-1")
    {
        return new ExistingContentState(
            Guid.NewGuid(),
            providerContentId,
            ExternalContentKind.Assignment,
            "Exercise 1",
            "Description",
            $"https://mock-moodle.local/content/{providerContentId}",
            new DateTimeOffset(2026, 9, 1, 12, 0, 0, TimeSpan.Zero));
    }

    private static CourseSnapshotItem CreateIncoming(string providerContentId)
    {
        return new CourseSnapshotItem(
            providerContentId,
            ExternalContentKind.Assignment,
            "Exercise 1",
            "Description",
            new Uri($"https://mock-moodle.local/content/{providerContentId}"),
            new DateTimeOffset(2026, 9, 1, 12, 0, 0, TimeSpan.Zero));
    }
}
