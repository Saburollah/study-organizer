namespace StudyOrganizer.Domain.Modules;

public sealed class StudyModule
{
    public Guid Id { get; }

    public Guid OwnerId { get; }

    public string Name { get; }

    public string? Code { get; }

    public string? Description { get; }

    public string? Color { get; }

    public DateTimeOffset CreatedAt { get; }

    public StudyModule(
        Guid ownerId,
        string name,
        string? code = null,
        string? description = null,
        string? color = null)
    {
        if (ownerId == Guid.Empty)
        {
            throw new ArgumentException(
                "Owner ID must not be empty.",
                nameof(ownerId));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Module name must not be empty.",
                nameof(name));
        }

        Id = Guid.NewGuid();
        OwnerId = ownerId;
        Name = name.Trim();
        Code = NormalizeOptionalValue(code);
        Description = NormalizeOptionalValue(description);
        Color = NormalizeOptionalValue(color);
        CreatedAt = DateTimeOffset.UtcNow;
    }

    private static string? NormalizeOptionalValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}