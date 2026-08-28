using StudyOrganizer.Domain.ExternalCourses;

namespace StudyOrganizer.Domain.Tests.ExternalCourses;

public sealed class ExternalCourseTests
{
    [Fact]
    public void Constructor_WithCanonicalIdentity_TrimsValues()
    {
        var now = new DateTimeOffset(2026, 8, 28, 8, 0, 0, TimeSpan.Zero);

        var course = new ExternalCourse(
            " mock-moodle ",
            " software-engineering-2026 ",
            " Software Engineering ",
            now);

        Assert.Equal("mock-moodle", course.ProviderKey);
        Assert.Equal("software-engineering-2026", course.ExternalCourseId);
        Assert.Equal("Software Engineering", course.Name);
        Assert.Null(course.ActiveScanRunId);
    }

    [Theory]
    [InlineData(" ", "course", "Course", "providerKey")]
    [InlineData("provider", " ", "Course", "externalCourseId")]
    [InlineData("provider", "course", " ", "name")]
    public void Constructor_WithBlankRequiredValue_Throws(
        string providerKey,
        string externalCourseId,
        string name,
        string parameterName)
    {
        var action = () => new ExternalCourse(
            providerKey,
            externalCourseId,
            name,
            DateTimeOffset.UtcNow);

        var exception = Assert.Throws<ArgumentException>(action);

        Assert.Equal(parameterName, exception.ParamName);
    }

    [Fact]
    public void Rename_WithCanonicalName_UpdatesTrimmedName()
    {
        var course = CreateCourse();

        course.Rename("  Revised course  ");

        Assert.Equal("Revised course", course.Name);
    }

    [Fact]
    public void Rename_WithBlankName_Throws()
    {
        var course = CreateCourse();

        var exception = Assert.Throws<ArgumentException>(() => course.Rename(" "));

        Assert.Equal("name", exception.ParamName);
    }

    [Fact]
    public void MarkScanStarted_WithNewRun_StoresActiveLease()
    {
        var course = CreateCourse();
        var runId = Guid.NewGuid();

        course.MarkScanStarted(runId);

        Assert.Equal(runId, course.ActiveScanRunId);
    }

    [Fact]
    public void MarkScanStarted_WhenLeaseIsAlreadyActive_Throws()
    {
        var course = CreateCourse();
        course.MarkScanStarted(Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() => course.MarkScanStarted(Guid.NewGuid()));
    }

    [Fact]
    public void MarkScanSucceeded_WithMatchingRun_ClearsLeaseAndStoresFinishTime()
    {
        var course = CreateCourse();
        var runId = Guid.NewGuid();
        var finishedAtUtc = new DateTimeOffset(2026, 8, 28, 9, 0, 0, TimeSpan.Zero);
        course.MarkScanStarted(runId);

        course.MarkScanSucceeded(runId, finishedAtUtc);

        Assert.Null(course.ActiveScanRunId);
        Assert.Equal(finishedAtUtc, course.LastSuccessfulScanAtUtc);
    }

    [Fact]
    public void MarkScanFailed_WithMatchingRun_ClearsLeaseWithoutChangingLastSuccess()
    {
        var course = CreateCourse();
        var successfulRunId = Guid.NewGuid();
        var firstFinishedAtUtc = new DateTimeOffset(2026, 8, 28, 9, 0, 0, TimeSpan.Zero);
        course.MarkScanStarted(successfulRunId);
        course.MarkScanSucceeded(successfulRunId, firstFinishedAtUtc);
        var failedRunId = Guid.NewGuid();
        course.MarkScanStarted(failedRunId);

        course.MarkScanFailed(failedRunId);

        Assert.Null(course.ActiveScanRunId);
        Assert.Equal(firstFinishedAtUtc, course.LastSuccessfulScanAtUtc);
    }

    [Fact]
    public void CompletingScan_WithNonMatchingRun_ThrowsAndLeavesLeaseActive()
    {
        var course = CreateCourse();
        var runId = Guid.NewGuid();
        course.MarkScanStarted(runId);

        Assert.Throws<InvalidOperationException>(() =>
            course.MarkScanSucceeded(Guid.NewGuid(), DateTimeOffset.UtcNow));

        Assert.Equal(runId, course.ActiveScanRunId);
    }

    [Fact]
    public void CourseSubscription_WithEmptyModuleId_Throws()
    {
        Assert.Throws<ArgumentException>(() => new CourseSubscription(
            Guid.NewGuid(), Guid.NewGuid(), Guid.Empty,
            DateTimeOffset.UtcNow));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void CourseSubscription_WithEmptyRequiredId_Throws(int emptyIdPosition)
    {
        var ownerId = emptyIdPosition == 0 ? Guid.Empty : Guid.NewGuid();
        var externalCourseId = emptyIdPosition == 1 ? Guid.Empty : Guid.NewGuid();
        var moduleId = emptyIdPosition == 2 ? Guid.Empty : Guid.NewGuid();

        Assert.Throws<ArgumentException>(() => new CourseSubscription(
            ownerId,
            externalCourseId,
            moduleId,
            DateTimeOffset.UtcNow));
    }

    [Fact]
    public void CourseSubscription_WithValidIdentity_CreatesSubscription()
    {
        var ownerId = Guid.NewGuid();
        var externalCourseId = Guid.NewGuid();
        var moduleId = Guid.NewGuid();
        var createdAtUtc = new DateTimeOffset(2026, 8, 28, 8, 0, 0, TimeSpan.Zero);

        var subscription = new CourseSubscription(
            ownerId,
            externalCourseId,
            moduleId,
            createdAtUtc);

        Assert.NotEqual(Guid.Empty, subscription.Id);
        Assert.Equal(ownerId, subscription.OwnerId);
        Assert.Equal(externalCourseId, subscription.ExternalCourseId);
        Assert.Equal(moduleId, subscription.ModuleId);
        Assert.Equal(createdAtUtc, subscription.CreatedAtUtc);
    }

    private static ExternalCourse CreateCourse()
    {
        return new ExternalCourse(
            "mock-moodle",
            "software-engineering-2026",
            "Software Engineering",
            new DateTimeOffset(2026, 8, 28, 8, 0, 0, TimeSpan.Zero));
    }
}
