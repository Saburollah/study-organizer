namespace StudyOrganizer.Domain.ExternalCourses;

public sealed class SubscriptionContentState
{
    public Guid Id { get; }

    public Guid CourseSubscriptionId { get; }

    public Guid ExternalCourseId { get; }

    public Guid ExternalLearningContentId { get; }

    public SubscriptionContentStateStatus Status { get; private set; }

    public Guid? StudyTaskId { get; private set; }

    public ContentSignature ConfirmedSignature { get; private set; } = null!;

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    private SubscriptionContentState()
    {
    }

    public SubscriptionContentState(
        Guid courseSubscriptionId,
        Guid externalCourseId,
        Guid externalLearningContentId,
        Guid studyTaskId,
        ContentSignature confirmedSignature,
        DateTimeOffset createdAt)
    {
        Id = Guid.NewGuid();
        CourseSubscriptionId = ValidateId(
            courseSubscriptionId,
            nameof(courseSubscriptionId));
        ExternalCourseId = ValidateId(
            externalCourseId,
            nameof(externalCourseId));
        ExternalLearningContentId = ValidateId(
            externalLearningContentId,
            nameof(externalLearningContentId));
        Status = SubscriptionContentStateStatus.Imported;
        StudyTaskId = ValidateId(studyTaskId, nameof(studyTaskId));
        ConfirmedSignature = confirmedSignature
            ?? throw new ArgumentNullException(
                nameof(confirmedSignature));
        CreatedAt = createdAt;
    }

    public void Dismiss(DateTimeOffset dismissedAt)
    {
        if (Status == SubscriptionContentStateStatus.Dismissed)
        {
            return;
        }

        Status = SubscriptionContentStateStatus.Dismissed;
        StudyTaskId = null;
        UpdatedAt = dismissedAt;
    }

    public void Restore(
        Guid studyTaskId,
        ContentSignature currentSignature,
        DateTimeOffset restoredAt)
    {
        if (Status != SubscriptionContentStateStatus.Dismissed)
        {
            throw new InvalidOperationException(
                "Only a dismissed import can be restored.");
        }

        var validatedStudyTaskId = ValidateId(
            studyTaskId,
            nameof(studyTaskId));
        var validatedSignature = currentSignature
            ?? throw new ArgumentNullException(
                nameof(currentSignature));

        Status = SubscriptionContentStateStatus.Imported;
        StudyTaskId = validatedStudyTaskId;
        ConfirmedSignature = validatedSignature;
        UpdatedAt = restoredAt;
    }

    public void ConfirmSignature(
        ContentSignature signature,
        DateTimeOffset confirmedAt)
    {
        if (Status != SubscriptionContentStateStatus.Imported)
        {
            throw new InvalidOperationException(
                "Only an imported state can confirm a signature.");
        }

        ArgumentNullException.ThrowIfNull(signature);

        if (ConfirmedSignature == signature)
        {
            return;
        }

        ConfirmedSignature = signature;
        UpdatedAt = confirmedAt;
    }

    private static Guid ValidateId(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "Subscription Content State IDs must not be empty.",
                parameterName);
        }

        return value;
    }
}
