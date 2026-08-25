namespace StudyOrganizer.Application.ExternalCourses;

public sealed class CourseSourceSnapshot
{
    public IReadOnlyList<CourseSourceItem> Items { get; }

    public CourseSourceSnapshot(
        IReadOnlyList<CourseSourceItem> items)
    {
        Items = items
            ?? throw new ArgumentNullException(nameof(items));
    }
}
