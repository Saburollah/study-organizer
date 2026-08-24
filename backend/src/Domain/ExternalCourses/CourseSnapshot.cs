namespace StudyOrganizer.Domain.ExternalCourses;

public sealed class CourseSnapshot
{
    public Guid Id { get; }

    public Guid ExternalCourseId { get; }

    public Guid ScanRunId { get; }

    public DateTimeOffset ObservedAt { get; }

    public bool IsCurrent { get; private set; }

    public CourseSnapshot(
        Guid externalCourseId,
        Guid scanRunId,
        DateTimeOffset observedAt)
    {
        Id = Guid.NewGuid();
        ExternalCourseId = ValidateId(
            externalCourseId,
            nameof(externalCourseId));
        ScanRunId = ValidateId(
            scanRunId,
            nameof(scanRunId));
        ObservedAt = observedAt;
        IsCurrent = true;
    }

    public void MarkSuperseded()
    {
        IsCurrent = false;
    }

    private static Guid ValidateId(
        Guid value,
        string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "Course Snapshot IDs must not be empty.",
                parameterName);
        }

        return value;
    }
}
