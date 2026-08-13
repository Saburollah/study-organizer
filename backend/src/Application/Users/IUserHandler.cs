namespace StudyOrganizer.Application.Users;

public interface IUserHandler
{
    Task<UserResult> RegisterAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default);

    Task<UserLoginResult> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default);

    Task<ChangePasswordResult> ChangePasswordAsync(
        Guid userId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default);
}
