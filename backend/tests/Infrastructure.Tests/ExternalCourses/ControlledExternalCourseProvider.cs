using StudyOrganizer.Application.ExternalCourses;
using StudyOrganizer.Domain.ExternalCourses;

namespace StudyOrganizer.Infrastructure.Tests.ExternalCourses;

public sealed class ControlledExternalCourseProvider : IExternalCourseProvider
{
    private static readonly string[] SupportedUrls =
    [
        "https://mock-moodle.local/courses/software-engineering-2026",
        "https://mock-moodle.local/course/view.php?id=se-2026"
    ];

    private CourseSnapshot _snapshot;
    private ExternalCourseProviderError? _failure;
    private Exception? _unexpectedFailure;
    private TaskCompletionSource? _blockedFetch;
    private TaskCompletionSource? _fetchStarted;

    private ControlledExternalCourseProvider(CourseSnapshot snapshot)
    {
        _snapshot = snapshot;
    }

    public string ProviderKey => "mock-moodle";

    public int FetchCount { get; private set; }

    public static ControlledExternalCourseProvider ForSoftwareEngineering()
    {
        return new ControlledExternalCourseProvider(
            new CourseSnapshot(
                "mock-moodle",
                "software-engineering-2026",
                true,
                []));
    }

    public bool CanHandle(Uri courseUri)
    {
        return SupportedUrls.Contains(courseUri.AbsoluteUri, StringComparer.Ordinal);
    }

    public Task<CourseDiscovery> DiscoverAsync(
        Uri courseUri,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!CanHandle(courseUri))
        {
            throw new ExternalCourseProviderException(
                ExternalCourseProviderError.UnsupportedUrl);
        }

        return Task.FromResult(
            new CourseDiscovery(
                ProviderKey,
                "software-engineering-2026",
                "Software Engineering"));
    }

    public async Task<CourseSnapshot> FetchSnapshotAsync(
        string externalCourseId,
        CancellationToken cancellationToken = default)
    {
        FetchCount++;
        _fetchStarted?.TrySetResult();

        var blockedFetch = _blockedFetch;
        if (blockedFetch is not null)
        {
            await blockedFetch.Task.WaitAsync(cancellationToken);
            _blockedFetch = null;
        }

        if (_failure is { } failure)
        {
            throw new ExternalCourseProviderException(failure);
        }

        if (_unexpectedFailure is { } unexpectedFailure)
        {
            throw unexpectedFailure;
        }

        return _snapshot;
    }

    public void SetSnapshot(CourseSnapshot snapshot)
    {
        _snapshot = snapshot;
        _failure = null;
        _unexpectedFailure = null;
    }

    public void SetFailure(ExternalCourseProviderError error)
    {
        _failure = error;
        _unexpectedFailure = null;
    }

    public void SetUnexpectedFailure(Exception exception)
    {
        _failure = null;
        _unexpectedFailure = exception;
    }

    public void BlockNextFetch()
    {
        _blockedFetch = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        _fetchStarted = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public Task WaitForFetchAsync() =>
        _fetchStarted?.Task
        ?? throw new InvalidOperationException("No fetch has been blocked.");

    public void ReleaseBlockedFetch()
    {
        _blockedFetch?.TrySetResult();
    }
}
