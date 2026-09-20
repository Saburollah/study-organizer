using StudyOrganizer.Domain.ExternalCourses;

namespace StudyOrganizer.Application.ExternalCourses;

public sealed record CourseDiscovery(
    string ProviderKey,
    string ExternalCourseId,
    string Name);

public sealed record CourseSnapshot(
    string ProviderKey,
    string ExternalCourseId,
    bool IsComplete,
    IReadOnlyList<CourseSnapshotItem> Contents);

public sealed record CourseSnapshotItem(
    string ProviderContentId,
    ExternalContentKind Kind,
    string Title,
    string? Description,
    Uri SourceUri,
    DateTimeOffset? StructuredDueDateUtc);

public interface IExternalCourseProvider
{
    string ProviderKey { get; }
    bool CanHandle(Uri courseUri);
    Task<CourseDiscovery> DiscoverAsync(Uri courseUri, CancellationToken cancellationToken = default);
    Task<CourseSnapshot> FetchSnapshotAsync(string externalCourseId, CancellationToken cancellationToken = default);
}

public enum ExternalCourseProviderError
{
    UnsupportedUrl,
    Timeout,
    AuthenticationRequired,
    InvalidResponse
}

public sealed class ExternalCourseProviderException : Exception
{
    public ExternalCourseProviderException(ExternalCourseProviderError error)
        : base(GetSafeMessage(error))
    {
        Error = error;
    }

    public ExternalCourseProviderError Error { get; }

    private static string GetSafeMessage(ExternalCourseProviderError error)
    {
        return error switch
        {
            ExternalCourseProviderError.UnsupportedUrl => "The course URL is not supported.",
            ExternalCourseProviderError.Timeout => "The external course provider timed out.",
            ExternalCourseProviderError.AuthenticationRequired => "Authentication is required to access the external course provider.",
            ExternalCourseProviderError.InvalidResponse => "The external course provider returned an invalid response.",
            _ => "The external course provider failed."
        };
    }
}
