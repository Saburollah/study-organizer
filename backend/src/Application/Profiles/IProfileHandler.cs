using StudyOrganizer.Domain.Users;

namespace StudyOrganizer.Application.Profiles;

public interface IProfileHandler
{
    Task<ProfileResult?> GetAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<ProfileResult?> UpdateAsync(
        Guid userId,
        string? firstName,
        string? lastName,
        DateOnly? dateOfBirth,
        ProfileGender? gender,
        CancellationToken cancellationToken = default);
}
