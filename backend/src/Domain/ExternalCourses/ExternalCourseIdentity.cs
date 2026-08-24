namespace StudyOrganizer.Domain.ExternalCourses;

public sealed record ExternalCourseIdentity
{
    public string SourceType { get; }

    public string SourceInstance { get; }

    public string ExternalCourseKey { get; }

    public ExternalCourseIdentity(
        string sourceType,
        string sourceInstance,
        string externalCourseKey)
    {
        SourceType = NormalizeRequired(
            sourceType,
            nameof(sourceType));

        SourceInstance = NormalizeRequired(
            sourceInstance,
            nameof(sourceInstance));

        ExternalCourseKey = NormalizeRequired(
            externalCourseKey,
            nameof(externalCourseKey));
    }

    private static string NormalizeRequired(
        string value,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "External Course Identity values must not be empty.",
                parameterName);
        }

        return value.Trim();
    }
}
