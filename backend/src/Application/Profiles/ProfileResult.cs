using StudyOrganizer.Domain.Users;

namespace StudyOrganizer.Application.Profiles;

public sealed record ProfileResult(
    Guid UserId,
    string Email,
    string? FirstName,
    string? LastName,
    DateOnly? DateOfBirth,
    ProfileGender? Gender);
