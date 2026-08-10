namespace StudyOrganizer.Application.Users;

public sealed record UserResult(
    bool Succeeded,
    Guid? UserId,
    IReadOnlyCollection<string> Errors);
