namespace StudyOrganizer.Domain.ExternalCourses;

public sealed record ScanRunCounts
{
    public int New { get; }

    public int Updated { get; }

    public int Unchanged { get; }

    public int Unavailable { get; }

    private ScanRunCounts()
    {
    }

    public ScanRunCounts(
        int newCount,
        int updatedCount,
        int unchangedCount,
        int unavailableCount)
    {
        New = ValidateCount(
            newCount,
            nameof(newCount));
        Updated = ValidateCount(
            updatedCount,
            nameof(updatedCount));
        Unchanged = ValidateCount(
            unchangedCount,
            nameof(unchangedCount));
        Unavailable = ValidateCount(
            unavailableCount,
            nameof(unavailableCount));
    }

    private static int ValidateCount(
        int value,
        string parameterName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "Scan Run counts must not be negative.");
        }

        return value;
    }
}
