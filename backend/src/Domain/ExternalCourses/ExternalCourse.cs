namespace StudyOrganizer.Domain.ExternalCourses;

public sealed class ExternalCourse
{
    public Guid Id { get; }

    public ExternalCourseIdentity Identity { get; }

    public string Name { get; }

    public ExternalCourseState State { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset? InactiveSince { get; private set; }

    public ExternalCourse(
        ExternalCourseIdentity identity,
        string name,
        DateTimeOffset createdAt)
    {
        Id = Guid.NewGuid();
        Identity = identity
            ?? throw new ArgumentNullException(nameof(identity));
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(
                    "External Course name must not be empty.",
                    nameof(name));
            }
        Name = name.Trim();
        State = ExternalCourseState.Inactive;
        CreatedAt = createdAt;
        InactiveSince = createdAt;
    }

    public void Activate()
    {
        State = ExternalCourseState.Active;
        InactiveSince = null;
    }

    public void Deactivate(DateTimeOffset inactiveAt)
    {
        if (State == ExternalCourseState.Inactive)
        {
            return;
        }

        State = ExternalCourseState.Inactive;
        InactiveSince = inactiveAt;
    }
}
