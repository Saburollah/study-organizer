namespace StudyOrganizer.Application.Authentication;

public sealed record AccessTokenResult(
    string Value,
    DateTimeOffset ExpiresAtUtc);
