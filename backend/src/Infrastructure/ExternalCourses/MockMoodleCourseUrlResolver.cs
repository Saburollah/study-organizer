using System.Globalization;
using StudyOrganizer.Application.ExternalCourses;
using StudyOrganizer.Domain.ExternalCourses;

namespace StudyOrganizer.Infrastructure.ExternalCourses;

public sealed class MockMoodleCourseUrlResolver
    : IExternalCourseUrlResolver
{
    public const string SourceType = "mock-moodle";
    public const string SourceInstance =
        "https://example.test/mock-moodle";

    private const string CoursePathPrefix =
        "/mock-moodle/course/";

    public ResolvedExternalCourse? Resolve(string courseUrl)
    {
        if (!Uri.TryCreate(
                courseUrl,
                UriKind.Absolute,
                out var uri)
            || !string.Equals(
                uri.Scheme,
                Uri.UriSchemeHttps,
                StringComparison.OrdinalIgnoreCase)
            || !string.Equals(
                uri.Host,
                "example.test",
                StringComparison.OrdinalIgnoreCase)
            || !uri.IsDefaultPort
            || !string.IsNullOrEmpty(uri.UserInfo)
            || !uri.AbsolutePath.StartsWith(
                CoursePathPrefix,
                StringComparison.Ordinal))
        {
            return null;
        }

        var encodedCourseKey = uri.AbsolutePath[
            CoursePathPrefix.Length..].TrimEnd('/');

        if (encodedCourseKey.Length == 0
            || encodedCourseKey.Contains('/'))
        {
            return null;
        }

        string courseKey;
        try
        {
            courseKey = Uri.UnescapeDataString(encodedCourseKey);
        }
        catch (UriFormatException)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(courseKey)
            || courseKey.Length > 450
            || courseKey is "." or ".."
            || courseKey.Contains('/')
            || courseKey.Contains('\\')
            || courseKey.Any(char.IsControl))
        {
            return null;
        }

        var canonicalCourseUrl =
            $"{SourceInstance}/course/{Uri.EscapeDataString(courseKey)}";

        return new ResolvedExternalCourse(
            new ExternalCourseIdentity(
                SourceType,
                SourceInstance,
                courseKey),
            ToDisplayName(courseKey),
            canonicalCourseUrl);
    }

    public string? GetSafeContentUrl(
        ExternalCourseIdentity identity,
        string? sourceReference)
    {
        ArgumentNullException.ThrowIfNull(identity);

        if (!string.Equals(
                identity.SourceType,
                SourceType,
                StringComparison.Ordinal)
            || !string.Equals(
                identity.SourceInstance,
                SourceInstance,
                StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(sourceReference)
            || !Uri.TryCreate(
                new Uri(SourceInstance + "/", UriKind.Absolute),
                sourceReference,
                out var resolved)
            || !string.Equals(
                resolved.Scheme,
                Uri.UriSchemeHttps,
                StringComparison.OrdinalIgnoreCase)
            || !string.Equals(
                resolved.Host,
                "example.test",
                StringComparison.OrdinalIgnoreCase)
            || !resolved.IsDefaultPort
            || !string.IsNullOrEmpty(resolved.UserInfo))
        {
            return null;
        }

        return new UriBuilder(resolved)
        {
            Query = string.Empty,
            Fragment = string.Empty
        }.Uri.AbsoluteUri;
    }

    private static string ToDisplayName(string courseKey)
    {
        var words = courseKey
            .Replace('-', ' ')
            .Replace('_', ' ')
            .Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries
                | StringSplitOptions.TrimEntries);

        if (words.Length == 0)
        {
            return courseKey;
        }

        var textInfo = CultureInfo.InvariantCulture.TextInfo;
        return string.Join(
            ' ',
            words.Select(word => textInfo.ToTitleCase(
                word.ToLowerInvariant())));
    }
}
