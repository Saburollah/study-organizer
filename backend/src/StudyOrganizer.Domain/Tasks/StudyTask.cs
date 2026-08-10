namespace StudyOrganizer.Domain.Tasks;

public sealed class StudyTask
{
    public Guid Id { get; }

    public Guid ModuleId { get; }

    public string Title { get; }

    public string? Description { get; }

    public DateTimeOffset DueDate { get; }

    public StudyTaskStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public StudyTask(
        Guid moduleId,
        string title,
        DateTimeOffset dueDate,
        string? description = null)
    {
        if (moduleId == Guid.Empty)
        {
            throw new ArgumentException(
                "Module ID must not be empty.",
                nameof(moduleId));
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException(
                "Task title must not be empty.",
                nameof(title));
        }

        Id = Guid.NewGuid();
        ModuleId = moduleId;
        Title = title.Trim();
        Description = NormalizeOptionalValue(description);
        DueDate = dueDate;
        Status = StudyTaskStatus.Open;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = null;
    }

    public void Complete()
    {
        Status = StudyTaskStatus.Completed;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Reopen()
    {
        Status = StudyTaskStatus.Open;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static string? NormalizeOptionalValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}