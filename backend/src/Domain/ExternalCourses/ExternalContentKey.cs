namespace StudyOrganizer.Domain.ExternalCourses;

public sealed record ExternalContentKey
{
    public string Value { get; }

    public ExternalContentKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "External Content Key must not be empty.",
                nameof(value));
        }

        Value = value.Trim();
    }
}
