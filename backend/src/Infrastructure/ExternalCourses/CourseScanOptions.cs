namespace StudyOrganizer.Infrastructure.ExternalCourses;

public sealed record CourseScanOptions
{
    public TimeSpan LeaseDuration { get; }

    public TimeSpan Timeout { get; }

    public CourseScanOptions(
        TimeSpan leaseDuration,
        TimeSpan timeout)
    {
        if (leaseDuration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(leaseDuration));
        }

        if (timeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(timeout));
        }

        LeaseDuration = leaseDuration;
        Timeout = timeout;
    }
}
