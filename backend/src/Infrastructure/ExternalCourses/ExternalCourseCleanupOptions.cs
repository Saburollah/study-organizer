namespace StudyOrganizer.Infrastructure.ExternalCourses;

public sealed record ExternalCourseCleanupOptions
{
    public TimeSpan RetentionPeriod { get; }

    public TimeSpan Interval { get; }

    public ExternalCourseCleanupOptions(
        TimeSpan retentionPeriod,
        TimeSpan interval)
    {
        if (retentionPeriod <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(retentionPeriod));
        }

        if (interval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(interval));
        }

        RetentionPeriod = retentionPeriod;
        Interval = interval;
    }
}
