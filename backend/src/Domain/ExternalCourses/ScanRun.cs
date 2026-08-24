namespace StudyOrganizer.Domain.ExternalCourses;

public sealed class ScanRun
{
    public Guid Id { get; }

    public Guid ExternalCourseId { get; }

    public ScanRunStatus Status { get; private set; }

    public DateTimeOffset StartedAt { get; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public DateTimeOffset LeaseExpiresAt { get; }

    public Guid? ActivationSubscriptionId { get; private set; }

    public ScanRunErrorCode? ErrorCode { get; private set; }

    public ScanRunCounts Counts { get; private set; } = null!;

    private ScanRun()
    {
    }

    public ScanRun(
        Guid externalCourseId,
        DateTimeOffset startedAt,
        DateTimeOffset leaseExpiresAt,
        Guid? activationSubscriptionId = null)
    {
        Id = Guid.NewGuid();
        if (externalCourseId == Guid.Empty)
        {
            throw new ArgumentException(
                "External Course ID must not be empty.",
                nameof(externalCourseId));
        }

        ExternalCourseId = externalCourseId;
        Status = ScanRunStatus.Running;
        StartedAt = startedAt;
        if (leaseExpiresAt <= startedAt)
        {
            throw new ArgumentOutOfRangeException(
                nameof(leaseExpiresAt),
                "Lease expiry must be after the scan start.");
        }
        LeaseExpiresAt = leaseExpiresAt;
        if (activationSubscriptionId == Guid.Empty)
        {
            throw new ArgumentException(
                "Activation Subscription ID must not be empty.",
                nameof(activationSubscriptionId));
        }
        ActivationSubscriptionId =
            activationSubscriptionId;
        Counts = new ScanRunCounts(0, 0, 0, 0);
    }

    public void Cancel(DateTimeOffset completedAt)
    {
        EnsureRunning();
        ValidateCompletionTime(completedAt);
        Status = ScanRunStatus.Cancelled;
        CompletedAt = completedAt;
        ErrorCode = null;
        ActivationSubscriptionId = null;
    }

    public void Expire(DateTimeOffset completedAt)
    {
        EnsureRunning();
        ValidateCompletionTime(completedAt);
        if (completedAt < LeaseExpiresAt)
        {
            throw new ArgumentOutOfRangeException(
                nameof(completedAt),
                "A Scan Run cannot expire before its lease.");
        }
        Status = ScanRunStatus.Expired;
        CompletedAt = completedAt;
        ErrorCode = ScanRunErrorCode.Timeout;
        ActivationSubscriptionId = null;
    }

    public void Fail(
        ScanRunErrorCode errorCode,
        DateTimeOffset completedAt)
    {
        EnsureRunning();
        ValidateCompletionTime(completedAt);
        if (!Enum.IsDefined(errorCode))
        {
            throw new ArgumentOutOfRangeException(
                nameof(errorCode),
                "Unknown Scan Run error code.");
        }
        Status = ScanRunStatus.Failed;
        CompletedAt = completedAt;
        ErrorCode = errorCode;
        ActivationSubscriptionId = null;
    }

    public void Succeed(
        ScanRunCounts counts,
        DateTimeOffset completedAt)
    {
        EnsureRunning();
        ValidateCompletionTime(completedAt);
        ArgumentNullException.ThrowIfNull(counts);
        Status = ScanRunStatus.Succeeded;
        CompletedAt = completedAt;
        Counts = counts;
        ErrorCode = null;
        ActivationSubscriptionId = null;
    }

    private void EnsureRunning()
    {
        if (Status != ScanRunStatus.Running)
        {
            throw new InvalidOperationException(
                "Only a running Scan Run can be completed.");
        }
    }

    private void ValidateCompletionTime(
        DateTimeOffset completedAt)
    {
        if (completedAt < StartedAt)
        {
            throw new ArgumentOutOfRangeException(
                nameof(completedAt),
                "Completion time must not be before the scan start.");
        }
    }
}
