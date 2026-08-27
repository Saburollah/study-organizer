using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace StudyOrganizer.Domain.ExternalCourses;

public sealed record ContentSignature
{
    public const int CurrentVersion = 1;

    public int Version { get; }

    public string Hash { get; }

    private ContentSignature(int version, string hash)
    {
        Version = version;
        Hash = hash;
    }

    public ContentSignature Copy()
    {
        return new ContentSignature(Version, Hash);
    }

    public static ContentSignature Compute(
        ExternalLearningContentType type,
        string title,
        DateTimeOffset? dueDate,
        string? mediaType,
        string? sourceReference,
        ExternalLearningContentAvailability availability)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException(
                "Title must not be empty.",
                nameof(title));
        }

        var payload = new StringBuilder();

        AppendField(
            payload,
            CurrentVersion.ToString(
                CultureInfo.InvariantCulture));
        AppendField(
            payload,
            ((int)type).ToString(
                CultureInfo.InvariantCulture));
        AppendField(payload, title.Trim());
        AppendField(
            payload,
            dueDate?.ToUniversalTime().ToString(
                "O",
                CultureInfo.InvariantCulture));
        AppendField(payload, NormalizeOptional(mediaType));
        AppendField(
            payload,
            NormalizeOptional(sourceReference));
        AppendField(
            payload,
            ((int)availability).ToString(
                CultureInfo.InvariantCulture));

        var hashBytes = SHA256.HashData(
            Encoding.UTF8.GetBytes(payload.ToString()));

        return new ContentSignature(
            CurrentVersion,
            Convert.ToHexString(hashBytes)
                .ToLowerInvariant());
    }

    private static void AppendField(
        StringBuilder payload,
        string? value)
    {
        if (value is null)
        {
            payload.Append("-1:;");
            return;
        }

        var normalized =
            value.Normalize(NormalizationForm.FormC);

        payload
            .Append(Encoding.UTF8.GetByteCount(normalized))
            .Append(':')
            .Append(normalized)
            .Append(';');
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
