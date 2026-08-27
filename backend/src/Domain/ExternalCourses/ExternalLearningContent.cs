namespace StudyOrganizer.Domain.ExternalCourses;

public sealed class ExternalLearningContent
{
    public Guid Id { get; }

    public Guid ExternalCourseId { get; }

    public ExternalContentKey ExternalContentKey { get; } = null!;

    public ExternalLearningContentType Type { get; private set; }

    public string Title { get; private set; } = null!;

    public DateTimeOffset? DueDate { get; private set; }

    public string? MediaType { get; private set; }

    public string? SourceReference { get; private set; }

    public ExternalLearningContentAvailability Availability { get; private set; }

    public ContentSignature Signature { get; private set; } = null!;

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public DateTimeOffset? MetadataPurgedAt { get; private set; }

    private ExternalLearningContent()
    {
    }

    public ExternalLearningContent(
        Guid externalCourseId,
        ExternalContentKey externalContentKey,
        ExternalLearningContentType type,
        string title,
        DateTimeOffset createdAt,
        DateTimeOffset? dueDate,
        string? mediaType,
        string? sourceReference)
    {
        Id = Guid.NewGuid();
        if (externalCourseId == Guid.Empty)
        {
            throw new ArgumentException(
                "External Course ID must not be empty.",
                nameof(externalCourseId));
        }
        ExternalCourseId = externalCourseId;
        ExternalContentKey = externalContentKey
            ?? throw new ArgumentNullException(
                nameof(externalContentKey));
        Type = type;
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException(
                "External Learning Content title must not be empty.",
                nameof(title));
        }
        Title = title.Trim();
        CreatedAt = createdAt;
        DueDate = dueDate;
        MediaType = NormalizeOptionalValue(mediaType);
        SourceReference =
            NormalizeOptionalValue(sourceReference);
        Availability =
            ExternalLearningContentAvailability.Available;
        Signature = ContentSignature.Compute(
            Type,
            Title,
            DueDate,
            MediaType,
            SourceReference,
            Availability);
        MetadataPurgedAt = null;
    }

    public void UpdateMetadata(
        ExternalLearningContentType type,
        string title,
        DateTimeOffset? dueDate,
        string? mediaType,
        string? sourceReference,
        DateTimeOffset updatedAt)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException(
                "External Learning Content title must not be empty.",
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
            Availability);
        UpdatedAt = updatedAt;
        MetadataPurgedAt = null;
    }

    public void MarkAvailable(DateTimeOffset updatedAt)
    {
        if (Availability ==
            ExternalLearningContentAvailability.Available)
        {
            return;
        }

        Availability =
            ExternalLearningContentAvailability.Available;
        Signature = ContentSignature.Compute(
            Type,
            Title,
            DueDate,
            MediaType,
            SourceReference,
            Availability);
        UpdatedAt = updatedAt;
    }

    public void MarkUnavailable(DateTimeOffset updatedAt)
    {
        if (Availability ==
            ExternalLearningContentAvailability.Unavailable)
        {
            return;
        }

        Availability =
            ExternalLearningContentAvailability.Unavailable;
        Signature = ContentSignature.Compute(
            Type,
            Title,
            DueDate,
            MediaType,
            SourceReference,
            Availability);
        UpdatedAt = updatedAt;
    }

    public void PurgeMetadata(DateTimeOffset purgedAt)
    {
        if (MetadataPurgedAt.HasValue)
        {
            return;
        }

        Type = ExternalLearningContentType.Activity;
        Title = "External content metadata purged";
        DueDate = null;
        MediaType = null;
        SourceReference = null;
        Availability =
            ExternalLearningContentAvailability.Unavailable;
        Signature = ContentSignature.Compute(
            Type,
            Title,
            DueDate,
            MediaType,
            SourceReference,
            Availability);
        UpdatedAt = purgedAt;
        MetadataPurgedAt = purgedAt;
    }

    private static string? NormalizeOptionalValue(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
