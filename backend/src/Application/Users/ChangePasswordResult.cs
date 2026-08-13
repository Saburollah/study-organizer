namespace StudyOrganizer.Application.Users;

public sealed record ChangePasswordResult(
    bool Succeeded,
    IReadOnlyCollection<string> Errors);
