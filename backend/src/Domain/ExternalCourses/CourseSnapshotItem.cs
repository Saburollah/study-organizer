namespace StudyOrganizer.Domain.ExternalCourses;

public sealed class CourseSnapshotItem
{
    public Guid CourseSnapshotId { get; }

    public Guid ExternalCourseId { get; }

    public Guid ExternalLearningContentId { get; }

    public ExternalContentKey ExternalContentKey { get; }

    public ExternalLearningContentType Type { get; }

    public string Title { get; }

    public DateTimeOffset? DueDate { get; }

    public string? MediaType { get; }

    public string? SourceReference { get; }

    public ContentSignature Signature { get; }

    public CourseSnapshotItem(
        Guid courseSnapshotId,
        Guid externalCourseId,
        Guid externalLearningContentId,
        ExternalContentKey externalContentKey,
        ExternalLearningContentType type,
        string title,
        DateTimeOffset? dueDate,
        string? mediaType,
        string? sourceReference)
    {
        CourseSnapshotId = ValidateId(
            courseSnapshotId,
            nameof(courseSnapshotId));
        ExternalCourseId = ValidateId(
            externalCourseId,
            nameof(externalCourseId));
        ExternalLearningContentId = ValidateId(
            externalLearningContentId,
            nameof(externalLearningContentId));
        ExternalContentKey = externalContentKey
            ?? throw new ArgumentNullException(
                nameof(externalContentKey));

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException(
                "Course Snapshot Item title must not be empty.",
                nameof(title));
        }

        Type = type;
        Title = title.Trim();
        DueDate = dueDate;
        MediaType = NormalizeOptionalValue(mediaType);
        SourceReference =
            NormalizeOptionalValue(sourceReference);
        Signature = ContentSignature.Compute(
            Type,
            Title,
            DueDate,
            MediaType,
            SourceReference,
            ExternalLearningContentAvailability.Available);
    }

    private static Guid ValidateId(
        Guid value,
        string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "Course Snapshot Item IDs must not be empty.",
                parameterName);
        }

        return value;
    }

    private static string? NormalizeOptionalValue(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
