namespace StudyOrganizer.Application.Users;

public interface IUserHandler
{
    Task<UserResult> RegisterAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default);
}
