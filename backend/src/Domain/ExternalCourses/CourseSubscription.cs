namespace StudyOrganizer.Domain.ExternalCourses;

public sealed class CourseSubscription
{
    public Guid Id { get; private set; }

    public Guid OwnerId { get; private set; }

    public Guid ExternalCourseId { get; private set; }

    public Guid ModuleId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    private CourseSubscription()
    {
    }

    public CourseSubscription(
        Guid ownerId,
        Guid externalCourseId,
        Guid moduleId,
        DateTimeOffset createdAtUtc)
    {
        EnsureNotEmpty(ownerId, nameof(ownerId));
        EnsureNotEmpty(externalCourseId, nameof(externalCourseId));
        EnsureNotEmpty(moduleId, nameof(moduleId));

        Id = Guid.NewGuid();
        OwnerId = ownerId;
        ExternalCourseId = externalCourseId;
        ModuleId = moduleId;
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
