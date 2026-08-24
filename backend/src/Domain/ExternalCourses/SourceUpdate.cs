namespace StudyOrganizer.Domain.ExternalCourses;

public sealed class SourceUpdate
{
    public Guid Id { get; }

    public Guid SubscriptionContentStateId { get; }

    public ContentSignature DetectedSignature { get; private set; } = null!;

    public DateTimeOffset DetectedAt { get; private set; }

    public Guid? DetectedByScanRunId { get; private set; }

    private SourceUpdate()
    {
    }

    public SourceUpdate(
        Guid subscriptionContentStateId,
        ContentSignature detectedSignature,
        DateTimeOffset detectedAt,
        Guid? detectedByScanRunId = null)
    {
        Id = Guid.NewGuid();
        SubscriptionContentStateId = ValidateId(
            subscriptionContentStateId,
            nameof(subscriptionContentStateId));
        DetectedSignature = detectedSignature
            ?? throw new ArgumentNullException(
                nameof(detectedSignature));
        DetectedAt = detectedAt;
        DetectedByScanRunId = ValidateOptionalId(
            detectedByScanRunId,
            nameof(detectedByScanRunId));
    }

    public void Refresh(
        ContentSignature detectedSignature,
        DateTimeOffset detectedAt,
        Guid? detectedByScanRunId = null)
    {
        ArgumentNullException.ThrowIfNull(detectedSignature);

        var validatedScanRunId = ValidateOptionalId(
            detectedByScanRunId,
            nameof(detectedByScanRunId));

        if (DetectedSignature == detectedSignature)
        {
            return;
        }

        DetectedSignature = detectedSignature;
        DetectedAt = detectedAt;
        DetectedByScanRunId = validatedScanRunId;
    }

    private static Guid ValidateId(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "Source Update IDs must not be empty.",
                parameterName);
        }

        return value;
    }

    private static Guid? ValidateOptionalId(
        Guid? value,
        string parameterName)
    {
        return value.HasValue
            ? ValidateId(value.Value, parameterName)
            : null;
    }
}
