using StudyOrganizer.Domain.ExternalCourses;

namespace StudyOrganizer.Domain.Tests.ExternalCourses;

public sealed class ScanRunTests
{
    [Fact]
    public void Constructor_WithValidValues_CreatesRunningScan()
    {
        // Arrange
        var externalCourseId = Guid.NewGuid();
        var activationSubscriptionId = Guid.NewGuid();

        var startedAt = new DateTimeOffset(
            2026,
            8,
            24,
            12,
            0,
            0,
            TimeSpan.Zero);

        var leaseExpiresAt = startedAt.AddMinutes(5);

        // Act
        var scanRun = new ScanRun(
            externalCourseId,
            startedAt,
            leaseExpiresAt,
            activationSubscriptionId);

        // Assert
        Assert.NotEqual(Guid.Empty, scanRun.Id);
        Assert.Equal(
            externalCourseId,
            scanRun.ExternalCourseId);
        Assert.Equal(ScanRunStatus.Running, scanRun.Status);
        Assert.Equal(startedAt, scanRun.StartedAt);
        Assert.Null(scanRun.CompletedAt);
        Assert.Equal(
            leaseExpiresAt,
            scanRun.LeaseExpiresAt);
        Assert.Equal(
            activationSubscriptionId,
            scanRun.ActivationSubscriptionId);
        Assert.Null(scanRun.ErrorCode);
        Assert.Equal(
            new ScanRunCounts(0, 0, 0, 0),
            scanRun.Counts);
    }

    [Fact]
    public void Constructor_WithEmptyExternalCourseId_ThrowsArgumentException()
    {
        // Act
        var action = () => new ScanRun(
            Guid.Empty,
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch.AddMinutes(5));

        // Assert
        var exception =
            Assert.Throws<ArgumentException>(action);

        Assert.Equal(
            "externalCourseId",
            exception.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithInvalidLease_ThrowsArgumentOutOfRangeException(
        int leaseOffsetMinutes)
    {
        // Arrange
        var startedAt = DateTimeOffset.UnixEpoch;

        // Act
        var action = () => new ScanRun(
            Guid.NewGuid(),
            startedAt,
            startedAt.AddMinutes(leaseOffsetMinutes));

        // Assert
        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(action);

        Assert.Equal(
            "leaseExpiresAt",
            exception.ParamName);
    }

    [Fact]
    public void Constructor_WithEmptyActivationSubscriptionId_ThrowsArgumentException()
    {
        // Act
        var action = () => new ScanRun(
            Guid.NewGuid(),
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch.AddMinutes(5),
            Guid.Empty);

        // Assert
        var exception =
            Assert.Throws<ArgumentException>(action);

        Assert.Equal(
            "activationSubscriptionId",
            exception.ParamName);
    }

    [Fact]
    public void Succeed_WhenRunning_StoresCountsAndCompletesScan()
    {
        // Arrange
        var startedAt = DateTimeOffset.UnixEpoch;
        var activationSubscriptionId = Guid.NewGuid();

        var scanRun = new ScanRun(
            Guid.NewGuid(),
            startedAt,
            startedAt.AddMinutes(5),
            activationSubscriptionId);

        var counts = new ScanRunCounts(3, 2, 5, 1);
        var completedAt = startedAt.AddMinutes(1);

        // Act
        scanRun.Succeed(counts, completedAt);

        // Assert
        Assert.Equal(
            ScanRunStatus.Succeeded,
            scanRun.Status);
        Assert.Equal(completedAt, scanRun.CompletedAt);
        Assert.Equal(counts, scanRun.Counts);
        Assert.Null(scanRun.ErrorCode);
        Assert.Null(scanRun.ActivationSubscriptionId);
    }

    [Fact]
    public void Fail_WhenRunning_StoresSafeErrorAndCompletesScan()
    {
        // Arrange
        var startedAt = DateTimeOffset.UnixEpoch;

        var scanRun = new ScanRun(
            Guid.NewGuid(),
            startedAt,
            startedAt.AddMinutes(5),
            Guid.NewGuid());

        var completedAt = startedAt.AddMinutes(1);

        // Act
        scanRun.Fail(
            ScanRunErrorCode.SourceUnreachable,
            completedAt);

        // Assert
        Assert.Equal(ScanRunStatus.Failed, scanRun.Status);
        Assert.Equal(completedAt, scanRun.CompletedAt);
        Assert.Equal(
            ScanRunErrorCode.SourceUnreachable,
            scanRun.ErrorCode);
        Assert.Equal(
            new ScanRunCounts(0, 0, 0, 0),
            scanRun.Counts);
        Assert.Null(scanRun.ActivationSubscriptionId);
    }

    [Fact]
    public void Cancel_WhenRunning_CompletesScanWithoutErrorCode()
    {
        // Arrange
        var startedAt = DateTimeOffset.UnixEpoch;

        var scanRun = new ScanRun(
            Guid.NewGuid(),
            startedAt,
            startedAt.AddMinutes(5),
            Guid.NewGuid());

        var completedAt = startedAt.AddMinutes(1);

        // Act
        scanRun.Cancel(completedAt);

        // Assert
        Assert.Equal(
            ScanRunStatus.Cancelled,
            scanRun.Status);
        Assert.Equal(completedAt, scanRun.CompletedAt);
        Assert.Null(scanRun.ErrorCode);
        Assert.Null(scanRun.ActivationSubscriptionId);
    }

    [Fact]
    public void Expire_WhenRunning_CompletesScanWithTimeoutError()
    {
        // Arrange
        var startedAt = DateTimeOffset.UnixEpoch;

        var scanRun = new ScanRun(
            Guid.NewGuid(),
            startedAt,
            startedAt.AddMinutes(5),
            Guid.NewGuid());

        var completedAt = startedAt.AddMinutes(6);

        // Act
        scanRun.Expire(completedAt);

        // Assert
        Assert.Equal(ScanRunStatus.Expired, scanRun.Status);
        Assert.Equal(completedAt, scanRun.CompletedAt);
        Assert.Equal(
            ScanRunErrorCode.Timeout,
            scanRun.ErrorCode);
        Assert.Null(scanRun.ActivationSubscriptionId);
    }

    [Fact]
    public void Succeed_AfterScanFailed_ThrowsInvalidOperationException()
    {
        // Arrange
        var startedAt = DateTimeOffset.UnixEpoch;
        var failedAt = startedAt.AddMinutes(1);

        var scanRun = new ScanRun(
            Guid.NewGuid(),
            startedAt,
            startedAt.AddMinutes(5));

        scanRun.Fail(
            ScanRunErrorCode.Unexpected,
            failedAt);

        // Act
        var action = () => scanRun.Succeed(
            new ScanRunCounts(1, 0, 0, 0),
            startedAt.AddMinutes(2));

        // Assert
        Assert.Throws<InvalidOperationException>(action);
        Assert.Equal(ScanRunStatus.Failed, scanRun.Status);
        Assert.Equal(failedAt, scanRun.CompletedAt);
        Assert.Equal(
            ScanRunErrorCode.Unexpected,
            scanRun.ErrorCode);
    }

    [Fact]
    public void Succeed_BeforeScanStarted_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var startedAt =
            DateTimeOffset.UnixEpoch.AddMinutes(1);

        var scanRun = new ScanRun(
            Guid.NewGuid(),
            startedAt,
            startedAt.AddMinutes(5));

        // Act
        var action = () => scanRun.Succeed(
            new ScanRunCounts(1, 0, 0, 0),
            startedAt.AddTicks(-1));

        // Assert
        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(action);

        Assert.Equal("completedAt", exception.ParamName);
        Assert.Equal(ScanRunStatus.Running, scanRun.Status);
        Assert.Null(scanRun.CompletedAt);
    }

    [Fact]
    public void Succeed_WithNullCounts_ThrowsArgumentNullException()
    {
        // Arrange
        var startedAt = DateTimeOffset.UnixEpoch;

        var scanRun = new ScanRun(
            Guid.NewGuid(),
            startedAt,
            startedAt.AddMinutes(5));

        // Act
        var action = () => scanRun.Succeed(
            null!,
            startedAt.AddMinutes(1));

        // Assert
        var exception =
            Assert.Throws<ArgumentNullException>(action);

        Assert.Equal("counts", exception.ParamName);
        Assert.Equal(ScanRunStatus.Running, scanRun.Status);
        Assert.Null(scanRun.CompletedAt);
    }

    [Fact]
    public void Fail_WithUnknownErrorCode_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var startedAt = DateTimeOffset.UnixEpoch;

        var scanRun = new ScanRun(
            Guid.NewGuid(),
            startedAt,
            startedAt.AddMinutes(5));

        // Act
        var action = () => scanRun.Fail(
            (ScanRunErrorCode)999,
            startedAt.AddMinutes(1));

        // Assert
        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(action);

        Assert.Equal("errorCode", exception.ParamName);
        Assert.Equal(ScanRunStatus.Running, scanRun.Status);
        Assert.Null(scanRun.CompletedAt);
    }

    [Fact]
    public void Expire_BeforeLeaseExpired_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var startedAt = DateTimeOffset.UnixEpoch;
        var leaseExpiresAt = startedAt.AddMinutes(5);

        var scanRun = new ScanRun(
            Guid.NewGuid(),
            startedAt,
            leaseExpiresAt);

        // Act
        var action = () => scanRun.Expire(
            leaseExpiresAt.AddTicks(-1));

        // Assert
        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(action);

        Assert.Equal("completedAt", exception.ParamName);
        Assert.Equal(ScanRunStatus.Running, scanRun.Status);
        Assert.Null(scanRun.CompletedAt);
    }
}
