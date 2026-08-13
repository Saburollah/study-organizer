using Microsoft.EntityFrameworkCore;
using StudyOrganizer.Application.Profiles;
using StudyOrganizer.Infrastructure.Identity;
using StudyOrganizer.Infrastructure.Persistence;
using StudyOrganizer.Domain.Users;

namespace StudyOrganizer.Infrastructure.Profiles;

public sealed class ProfileHandler(
    ApplicationDbContext dbContext)
    : IProfileHandler
{
    public async Task<ProfileResult?> GetAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(
                user => user.Id == userId,
                cancellationToken);

        return user is null
            ? null
            : ToResult(user);
    }

    public async Task<ProfileResult?> UpdateAsync(
        Guid userId,
        string? firstName,
        string? lastName,
        DateOnly? dateOfBirth,
        ProfileGender? gender,
        CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .FirstOrDefaultAsync(
                user => user.Id == userId,
                cancellationToken);

        if (user is null)
        {
            return null;
        }

        user.FirstName = Normalize(firstName);
        user.LastName = Normalize(lastName);
        user.DateOfBirth = dateOfBirth;
        user.Gender = gender;

        await dbContext.SaveChangesAsync(cancellationToken);

        return ToResult(user);
    }

    private static ProfileResult ToResult(
        ApplicationUser user)
    {
        if (string.IsNullOrWhiteSpace(user.Email))
        {
            throw new InvalidOperationException(
                "The user does not have an email address.");
        }

        return new ProfileResult(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.DateOfBirth,
            user.Gender);
    }

    private static string? Normalize(string? value)
    {
        var normalized = value?.Trim();

        return string.IsNullOrEmpty(normalized)
            ? null
            : normalized;
    }
}
