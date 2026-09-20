namespace StudyOrganizer.Domain.ExternalCourses;

public sealed class ExternalContent
{
    public Guid Id { get; private set; }

    public Guid ExternalCourseId { get; private set; }

    public string ProviderContentId { get; private set; } = null!;

    public ExternalContentKind Kind { get; private set; }

    public string Title { get; private set; } = null!;

    public string? Description { get; private set; }

    public string SourceUrl { get; private set; } = null!;

    public DateTimeOffset? StructuredDueDateUtc { get; private set; }

    public ExternalContentProcessingState ProcessingState { get; private set; }

    public ExternalContentReviewReason ReviewReason { get; private set; }

    public ExternalContentVisibility Visibility { get; private set; }

    public DateTimeOffset LastSeenAtUtc { get; private set; }

    private ExternalContent()
    {
    }

    private ExternalContent(
        Guid externalCourseId,
        string providerContentId,
        ExternalContentKind kind,
        string title,
        string? description,
        string sourceUrl,
        DateTimeOffset? structuredDueDateUtc,
        ExternalContentProcessingState processingState,
        ExternalContentReviewReason reviewReason,
        DateTimeOffset lastSeenAtUtc)
    {
        EnsureNotEmpty(externalCourseId, nameof(externalCourseId));

        Id = Guid.NewGuid();
        ExternalCourseId = externalCourseId;
        ProviderContentId = NormalizeRequiredValue(
            providerContentId,
            nameof(providerContentId));
        ApplySnapshot(
            kind,
            title,
            description,
            sourceUrl,
            structuredDueDateUtc,
            processingState,
            reviewReason,
            lastSeenAtUtc);
    }

    public static ExternalContent Create(
        Guid externalCourseId,
        string providerContentId,
        ExternalContentKind kind,
        string title,
        string? description,
        string sourceUrl,
        DateTimeOffset? structuredDueDateUtc,
        ExternalContentProcessingState processingState,
        ExternalContentReviewReason reviewReason,
        DateTimeOffset lastSeenAtUtc)
    {
        return new ExternalContent(
            externalCourseId,
            providerContentId,
            kind,
            title,
            description,
            sourceUrl,
            structuredDueDateUtc,
            processingState,
            reviewReason,
            lastSeenAtUtc);
    }

    public void ApplySnapshot(
        ExternalContentKind kind,
        string title,
        string? description,
        string sourceUrl,
        DateTimeOffset? structuredDueDateUtc,
        ExternalContentProcessingState processingState,
        ExternalContentReviewReason reviewReason,
        DateTimeOffset lastSeenAtUtc)
    {
        Kind = kind;
        Title = NormalizeRequiredValue(title, nameof(title));
        Description = NormalizeOptionalValue(description);
        SourceUrl = NormalizeRequiredValue(sourceUrl, nameof(sourceUrl));
        StructuredDueDateUtc = structuredDueDateUtc;
        ProcessingState = processingState;
        ReviewReason = reviewReason;
        Visibility = ExternalContentVisibility.Visible;
        LastSeenAtUtc = lastSeenAtUtc;
    }

    public void MarkNotVisible()
    {
        Visibility = ExternalContentVisibility.NotVisible;
    }

    private static void EnsureNotEmpty(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("ID must not be empty.", parameterName);
        }
    }

    private static string NormalizeRequiredValue(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value must not be empty.", parameterName);
        }

        return value.Trim();
    }

    private static string? NormalizeOptionalValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
