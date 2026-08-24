namespace StudyOrganizer.Domain.ExternalCourses;

public sealed class CourseSubscription
{
    public Guid Id { get; }

    public Guid StudyModuleId { get; }

    public Guid OwnerId { get; }

    public Guid ExternalCourseId { get; }

    public CourseSubscriptionState State { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset? ActivatedAt { get; private set; }

    public DateTimeOffset? EndedAt { get; private set; }

    private CourseSubscription()
    {
    }

    public CourseSubscription(
        Guid studyModuleId,
        Guid ownerId,
        Guid externalCourseId,
        DateTimeOffset createdAt)
    {
        Id = Guid.NewGuid();
        StudyModuleId = ValidateId(
            studyModuleId,
            nameof(studyModuleId));

        OwnerId = ValidateId(
            ownerId,
            nameof(ownerId));

        ExternalCourseId = ValidateId(
            externalCourseId,
            nameof(externalCourseId));
        State = CourseSubscriptionState.Pending;
        CreatedAt = createdAt;
    }

    private static Guid ValidateId(
        Guid value,
        string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "Course Subscription IDs must not be empty.",
                parameterName);
        }

        return value;
    }

    public void Activate(DateTimeOffset activatedAt)
    {
        if (State != CourseSubscriptionState.Pending)
        {
            throw new InvalidOperationException(
                "Only a pending Course Subscription can be activated.");
        }

        State = CourseSubscriptionState.Active;
        ActivatedAt = activatedAt;
    }

    public void End(DateTimeOffset endedAt)
    {
        if (State == CourseSubscriptionState.Ended)
        {
            return;
        }

        State = CourseSubscriptionState.Ended;
        EndedAt = endedAt;
    }

    public void BeginReactivation()
    {
        if (State != CourseSubscriptionState.Ended)
        {
            throw new InvalidOperationException(
                "Only an ended Course Subscription can begin reactivation.");
        }

        State = CourseSubscriptionState.Pending;
        ActivatedAt = null;
        EndedAt = null;
    }
}
