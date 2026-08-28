using StudyOrganizer.Domain.ExternalCourses;

namespace StudyOrganizer.Application.ExternalCourses;

public enum CourseContentChangeKind
{
    New,
    Changed,
    Unchanged,
    Missing
}

public sealed record ExistingContentState(
    Guid Id,
    string ProviderContentId,
    ExternalContentKind Kind,
    string Title,
    string? Description,
    string SourceUrl,
    DateTimeOffset? StructuredDueDateUtc);

public sealed record CourseContentChange(
    CourseContentChangeKind Kind,
    ExistingContentState? Existing,
    CourseSnapshotItem? Incoming);

public sealed record CourseSnapshotDiff(
    IReadOnlyList<CourseContentChange> Changes);

public sealed class InvalidCourseSnapshotException(string message)
    : Exception(message);

public static class CourseSnapshotDiffer
{
    public static CourseSnapshotDiff Compare(
        IReadOnlyCollection<ExistingContentState> existing,
        CourseSnapshot incoming)
    {
        var existingByProviderContentId = new Dictionary<string, ExistingContentState>(StringComparer.Ordinal);
        foreach (var content in existing)
        {
            existingByProviderContentId.Add(content.ProviderContentId, content);
        }

        var incomingIds = new HashSet<string>(StringComparer.Ordinal);
        var changes = new List<CourseContentChange>();

        foreach (var item in incoming.Contents)
        {
            if (!incomingIds.Add(item.ProviderContentId))
            {
                throw new InvalidCourseSnapshotException(
                    "Incoming snapshot contains duplicate provider content IDs.");
            }

            if (!existingByProviderContentId.TryGetValue(item.ProviderContentId, out var existingContent))
            {
                changes.Add(new CourseContentChange(CourseContentChangeKind.New, null, item));
                continue;
            }

            var changeKind = HasSameMutableValues(existingContent, item)
                ? CourseContentChangeKind.Unchanged
                : CourseContentChangeKind.Changed;
            changes.Add(new CourseContentChange(changeKind, existingContent, item));
        }

        foreach (var existingContent in existingByProviderContentId.Values)
        {
            if (!incomingIds.Contains(existingContent.ProviderContentId))
            {
                changes.Add(new CourseContentChange(CourseContentChangeKind.Missing, existingContent, null));
            }
        }

        var orderedChanges = changes
            .OrderBy(GetProviderContentId, StringComparer.Ordinal)
            .ToArray();

        return new CourseSnapshotDiff(orderedChanges);
    }

    private static bool HasSameMutableValues(
        ExistingContentState existing,
        CourseSnapshotItem incoming)
    {
        return existing.Kind == incoming.Kind
            && string.Equals(existing.Title, incoming.Title, StringComparison.Ordinal)
            && string.Equals(existing.Description, incoming.Description, StringComparison.Ordinal)
            && string.Equals(existing.SourceUrl, incoming.SourceUri.AbsoluteUri, StringComparison.Ordinal)
            && existing.StructuredDueDateUtc == incoming.StructuredDueDateUtc;
    }

    private static string GetProviderContentId(CourseContentChange change)
    {
        return change.Incoming?.ProviderContentId ?? change.Existing!.ProviderContentId;
    }
}
