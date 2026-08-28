namespace StudyOrganizer.Domain.ExternalCourses;

public sealed class ScanRun
{
    private static readonly HashSet<string> SupportedErrorCodes = new(
        StringComparer.Ordinal)
    {
        "external_timeout",
        "external_auth_required",
        "invalid_external_response",
        "unsupported_url"
    };

    public Guid Id { get; private set; }

    public Guid ExternalCourseId { get; private set; }

    public Guid RequestedByOwnerId { get; private set; }

    public ScanRunStatus Status { get; private set; }

    public DateTimeOffset StartedAtUtc { get; private set; }

    public DateTimeOffset? FinishedAtUtc { get; private set; }

    public string? ErrorCode { get; private set; }

    private ScanRun()
    {
    }

    public ScanRun(
        Guid externalCourseId,
        Guid requestedByOwnerId,
        DateTimeOffset startedAtUtc)
    {
        EnsureNotEmpty(externalCourseId, nameof(externalCourseId));
        EnsureNotEmpty(requestedByOwnerId, nameof(requestedByOwnerId));

        Id = Guid.NewGuid();
        ExternalCourseId = externalCourseId;
        RequestedByOwnerId = requestedByOwnerId;
        Status = ScanRunStatus.InProgress;
        StartedAtUtc = startedAtUtc;
    }

    public void Succeed(DateTimeOffset finishedAtUtc)
    {
        EnsureCanComplete(finishedAtUtc);

        Status = ScanRunStatus.Succeeded;
        FinishedAtUtc = finishedAtUtc;
    }

    public void Fail(string errorCode, DateTimeOffset finishedAtUtc)
    {
        EnsureCanComplete(finishedAtUtc);

        ErrorCode = NormalizeSupportedErrorCode(errorCode);
        Status = ScanRunStatus.Failed;
        FinishedAtUtc = finishedAtUtc;
    }

    private void EnsureCanComplete(DateTimeOffset finishedAtUtc)
    {
        if (Status != ScanRunStatus.InProgress)
        {
            throw new InvalidOperationException("Only an in-progress scan run can complete.");
        }

        if (finishedAtUtc < StartedAtUtc)
        {
            throw new ArgumentException(
                "Finished time must not be earlier than the start time.",
                nameof(finishedAtUtc));
        }
    }

    private static void EnsureNotEmpty(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("ID must not be empty.", parameterName);
        }
    }

    private static string NormalizeSupportedErrorCode(string errorCode)
    {
        if (string.IsNullOrWhiteSpace(errorCode))
        {
            throw new ArgumentException("Error code must not be empty.", nameof(errorCode));
        }

        var normalizedErrorCode = errorCode.Trim();

        if (!SupportedErrorCodes.Contains(normalizedErrorCode))
        {
            throw new ArgumentException(
                "Error code is not supported.",
                nameof(errorCode));
        }

        return normalizedErrorCode;
    }
}
