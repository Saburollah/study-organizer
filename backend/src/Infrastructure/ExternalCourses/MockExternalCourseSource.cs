using StudyOrganizer.Application.ExternalCourses;
using StudyOrganizer.Domain.ExternalCourses;

namespace StudyOrganizer.Infrastructure.ExternalCourses;

public sealed class MockExternalCourseSource
    : IExternalCourseSource
{
    private readonly object _gate = new();
    private readonly Dictionary<ExternalCourseIdentity, CourseScenario>
        _courses = [];

    public void RegisterCourse(
        ExternalCourseIdentity identity,
        string initialVersion,
        IReadOnlyDictionary<string, CourseSourceSnapshot> versions)
    {
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(versions);

        if (string.IsNullOrWhiteSpace(initialVersion))
        {
            throw new ArgumentException(
                "Initial version must not be empty.",
                nameof(initialVersion));
        }

        if (!versions.ContainsKey(initialVersion))
        {
            throw new ArgumentException(
                "Initial version must exist in the version catalog.",
                nameof(initialVersion));
        }

        lock (_gate)
        {
            if (!_courses.TryAdd(
                identity,
                new CourseScenario(initialVersion, versions)))
            {
                throw new InvalidOperationException(
                    "Mock course is already registered.");
            }
        }
    }

    public void UseVersion(
        ExternalCourseIdentity identity,
        string version)
    {
        ArgumentNullException.ThrowIfNull(identity);

        lock (_gate)
        {
            var scenario = GetScenario(identity);
            if (!scenario.Versions.ContainsKey(version))
            {
                throw new ArgumentException(
                    "Unknown mock course version.",
                    nameof(version));
            }

            scenario.CurrentVersion = version;
        }
    }

    public int GetFetchCount(ExternalCourseIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(identity);

        lock (_gate)
        {
            return GetScenario(identity).FetchCount;
        }
    }

    public void FailWith(
        ExternalCourseIdentity identity,
        ScanRunErrorCode errorCode)
    {
        ArgumentNullException.ThrowIfNull(identity);
        _ = new ExternalCourseSourceException(errorCode);

        lock (_gate)
        {
            GetScenario(identity).Failure = errorCode;
        }
    }

    public void ClearFailure(ExternalCourseIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(identity);

        lock (_gate)
        {
            GetScenario(identity).Failure = null;
        }
    }

    public Task<CourseSourceSnapshot> FetchSnapshotAsync(
        ExternalCourseIdentity identity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(identity);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            var scenario = GetScenario(identity);
            scenario.FetchCount++;
            if (scenario.Failure.HasValue)
            {
                throw new ExternalCourseSourceException(
                    scenario.Failure.Value);
            }

            var snapshot = scenario.Versions[scenario.CurrentVersion];
            if (scenario.NextVersion is not null)
            {
                scenario.CurrentVersion = scenario.NextVersion;
                scenario.NextVersion = null;
            }

            return Task.FromResult(snapshot);
        }
    }

    private CourseScenario GetScenario(
        ExternalCourseIdentity identity)
    {
        if (_courses.TryGetValue(identity, out var scenario))
        {
            return scenario;
        }

        if (string.Equals(
                identity.SourceType,
                MockMoodleCourseUrlResolver.SourceType,
                StringComparison.Ordinal)
            && string.Equals(
                identity.SourceInstance,
                MockMoodleCourseUrlResolver.SourceInstance,
                StringComparison.Ordinal))
        {
            scenario = CreateDefaultScenario(identity.ExternalCourseKey);
            _courses.Add(identity, scenario);
            return scenario;
        }

        throw new KeyNotFoundException(
            "Mock course is not registered.");
    }

    private static CourseScenario CreateDefaultScenario(string courseKey)
    {
        var title = courseKey
            .Replace('-', ' ')
            .Replace('_', ' ');

        var snapshot = new CourseSourceSnapshot(
        [
            new CourseSourceItem(
                new ExternalContentKey("reading-pdf"),
                ExternalLearningContentType.File,
                $"{title} PDF",
                null,
                "application/pdf",
                $"/mock-moodle/content/{Uri.EscapeDataString(courseKey)}/reading.pdf"),
            new CourseSourceItem(
                new ExternalContentKey("reference-link"),
                ExternalLearningContentType.Link,
                $"{title} reference",
                null,
                null,
                $"/mock-moodle/content/{Uri.EscapeDataString(courseKey)}/reference"),
            new CourseSourceItem(
                new ExternalContentKey("practice-activity"),
                ExternalLearningContentType.Activity,
                $"{title} activity",
                null,
                null,
                null)
        ]);

        var updatedSnapshot = new CourseSourceSnapshot(
        [
            .. snapshot.Items,
            new CourseSourceItem(
                new ExternalContentKey("project-brief"),
                ExternalLearningContentType.Activity,
                $"{title} project brief",
                null,
                null,
                null)
        ]);

        return new CourseScenario(
            "initial",
            new Dictionary<string, CourseSourceSnapshot>
            {
                ["initial"] = snapshot,
                ["updated"] = updatedSnapshot
            },
            "updated");
    }

    private sealed class CourseScenario(
        string currentVersion,
        IReadOnlyDictionary<string, CourseSourceSnapshot> versions,
        string? nextVersion = null)
    {
        public string CurrentVersion { get; set; } = currentVersion;

        public IReadOnlyDictionary<string, CourseSourceSnapshot> Versions
        {
            get;
        } = versions;

        public int FetchCount { get; set; }

        public ScanRunErrorCode? Failure { get; set; }

        public string? NextVersion { get; set; } = nextVersion;
    }
}
