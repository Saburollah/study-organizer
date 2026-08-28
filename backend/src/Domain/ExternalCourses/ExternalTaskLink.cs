namespace StudyOrganizer.Domain.ExternalCourses;

public sealed class ExternalTaskLink
{
    public Guid Id { get; private set; }

    public Guid CourseSubscriptionId { get; private set; }

    public Guid ExternalContentId { get; private set; }

    public Guid TaskId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    private ExternalTaskLink()
    {
    }

    public ExternalTaskLink(
        Guid courseSubscriptionId,
        Guid externalContentId,
        Guid taskId,
        DateTimeOffset createdAtUtc)
    {
        EnsureNotEmpty(courseSubscriptionId, nameof(courseSubscriptionId));
        EnsureNotEmpty(externalContentId, nameof(externalContentId));
        EnsureNotEmpty(taskId, nameof(taskId));

        Id = Guid.NewGuid();
        CourseSubscriptionId = courseSubscriptionId;
        ExternalContentId = externalContentId;
        TaskId = taskId;
        CreatedAtUtc = createdAtUtc;
    }

    private static void EnsureNotEmpty(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("ID must not be empty.", parameterName);
        }
    }
}
