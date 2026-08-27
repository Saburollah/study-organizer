namespace StudyOrganizer.Application.ExternalCourses;

public sealed class ExternalCourseSourcePayload
{
    public IReadOnlyList<CourseSourceItem> Items { get; }

    public ExternalCourseSourcePayload(
        IReadOnlyList<CourseSourceItem> items)
    {
        Items = items
            ?? throw new ArgumentNullException(nameof(items));
    }
}
