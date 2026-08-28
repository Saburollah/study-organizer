namespace StudyOrganizer.Domain.ExternalCourses;

public sealed class ExternalCourse
{
    public Guid Id { get; private set; }

    public string ProviderKey { get; private set; } = null!;

    public string ExternalCourseId { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public Guid? ActiveScanRunId { get; private set; }

    public DateTimeOffset? LastSuccessfulScanAtUtc { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    private ExternalCourse()
    {
    }

    public ExternalCourse(
        string providerKey,
        string externalCourseId,
        string name,
        DateTimeOffset createdAtUtc)
    {
        Id = Guid.NewGuid();
        ProviderKey = NormalizeRequiredValue(providerKey, nameof(providerKey));
        ExternalCourseId = NormalizeRequiredValue(
            externalCourseId,
            nameof(externalCourseId));
        Name = NormalizeRequiredValue(name, nameof(name));
        CreatedAtUtc = createdAtUtc;
    }

    public void Rename(string name)
    {
        Name = NormalizeRequiredValue(name, nameof(name));
    }

    public void MarkScanStarted(Guid scanRunId)
    {
        EnsureNotEmpty(scanRunId, nameof(scanRunId));

        if (ActiveScanRunId is not null)
        {
            throw new InvalidOperationException("A scan run is already active.");
        }

        ActiveScanRunId = scanRunId;
    }

    public void MarkScanSucceeded(Guid scanRunId, DateTimeOffset finishedAtUtc)
    {
        EnsureActiveRun(scanRunId);

        ActiveScanRunId = null;
        LastSuccessfulScanAtUtc = finishedAtUtc;
    }

    public void MarkScanFailed(Guid scanRunId)
    {
        EnsureActiveRun(scanRunId);

        ActiveScanRunId = null;
    }

    private void EnsureActiveRun(Guid scanRunId)
    {
        EnsureNotEmpty(scanRunId, nameof(scanRunId));

        if (ActiveScanRunId != scanRunId)
        {
            throw new InvalidOperationException("The scan run is not active for this course.");
        }
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
}
