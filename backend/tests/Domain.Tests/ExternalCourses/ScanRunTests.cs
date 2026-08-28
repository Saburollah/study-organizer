using StudyOrganizer.Domain.ExternalCourses;

namespace StudyOrganizer.Domain.Tests.ExternalCourses;

public sealed class ScanRunTests
{
    [Fact]
    public void Constructor_WithValidIdentity_StartsInProgress()
    {
        var externalCourseId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var startedAtUtc = new DateTimeOffset(2026, 8, 28, 8, 0, 0, TimeSpan.Zero);

        var run = new ScanRun(externalCourseId, ownerId, startedAtUtc);

        Assert.NotEqual(Guid.Empty, run.Id);
        Assert.Equal(externalCourseId, run.ExternalCourseId);
        Assert.Equal(ownerId, run.RequestedByOwnerId);
        Assert.Equal(ScanRunStatus.InProgress, run.Status);
        Assert.Equal(startedAtUtc, run.StartedAtUtc);
        Assert.Null(run.FinishedAtUtc);
        Assert.Null(run.ErrorCode);
    }

    [Fact]
    public void Constructor_WithEmptyRequiredId_Throws()
    {
        Assert.Throws<ArgumentException>(() => new ScanRun(
            Guid.Empty,
            Guid.NewGuid(),
            DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Succeed_CompletesInProgressRun()
    {
        var run = CreateRun();
        var finishedAtUtc = run.StartedAtUtc.AddSeconds(1);

        run.Succeed(finishedAtUtc);

        Assert.Equal(ScanRunStatus.Succeeded, run.Status);
        Assert.Equal(finishedAtUtc, run.FinishedAtUtc);
        Assert.Null(run.ErrorCode);
    }

    [Fact]
    public void Fail_StoresSafeCodeAndCompletesRun()
    {
        var run = new ScanRun(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        run.Fail("external_timeout", DateTimeOffset.UtcNow.AddSeconds(1));

        Assert.Equal(ScanRunStatus.Failed, run.Status);
        Assert.Equal("external_timeout", run.ErrorCode);
        Assert.NotNull(run.FinishedAtUtc);
    }

    [Fact]
    public void Fail_WithSurroundingWhitespace_NormalizesErrorCode()
    {
        var run = CreateRun();

        run.Fail(" external_timeout ", run.StartedAtUtc.AddSeconds(1));

        Assert.Equal("external_timeout", run.ErrorCode);
    }

    [Fact]
    public void Complete_WhenAlreadyTerminal_Throws()
    {
        var run = CreateRun();
        run.Succeed(run.StartedAtUtc.AddSeconds(1));

        Assert.Throws<InvalidOperationException>(() =>
            run.Fail("external_timeout", run.StartedAtUtc.AddSeconds(2)));
    }

    [Fact]
    public void Complete_BeforeStart_Throws()
    {
        var run = CreateRun();

        Assert.Throws<ArgumentException>(() => run.Succeed(run.StartedAtUtc.AddSeconds(-1)));
    }

    [Fact]
    public void Fail_WithBlankErrorCode_Throws()
    {
        var run = CreateRun();

        var exception = Assert.Throws<ArgumentException>(() =>
            run.Fail(" ", run.StartedAtUtc.AddSeconds(1)));

        Assert.Equal("errorCode", exception.ParamName);
    }

    private static ScanRun CreateRun()
    {
        return new ScanRun(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateTimeOffset(2026, 8, 28, 8, 0, 0, TimeSpan.Zero));
    }
}
