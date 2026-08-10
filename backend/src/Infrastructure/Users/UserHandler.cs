using Microsoft.AspNetCore.Identity;
using StudyOrganizer.Application.Users;
using StudyOrganizer.Infrastructure.Identity;

namespace StudyOrganizer.Infrastructure.Users;

public sealed class UserHandler(
    UserManager<ApplicationUser> userManager)
    : IUserHandler
{
    public async Task<UserResult> RegisterAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = new ApplicationUser
        {
            Email = email,
            UserName = email
        };

        var identityResult =
            await userManager.CreateAsync(user, password);

        if (!identityResult.Succeeded)
        {
            var errors = identityResult.Errors
                .Select(error => error.Description)
                .ToArray();

            return new UserResult(
                false,
                null,
                errors);
        }

        return new UserResult(
            true,
            user.Id,
            Array.Empty<string>());
    }
}
