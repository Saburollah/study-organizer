namespace StudyOrganizer.Application.Users;

public sealed record UserLoginResult(
    bool Succeeded,
    Guid? UserId,
    string? Email);
    