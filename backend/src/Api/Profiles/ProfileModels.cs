using System.ComponentModel.DataAnnotations;

namespace StudyOrganizer.Api.Profiles;

public sealed class UpdateProfileRequest
{
    [StringLength(100)]
    public string? FirstName { get; init; }

    [StringLength(100)]
    public string? LastName { get; init; }

    public DateOnly? DateOfBirth { get; init; }

    public string? Gender { get; init; }
}

public sealed record ProfileResponse(
    Guid UserId,
    string Email,
    string? FirstName,
    string? LastName,
    DateOnly? DateOfBirth,
    string? Gender);
