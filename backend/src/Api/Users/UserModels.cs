using System.ComponentModel.DataAnnotations;

namespace StudyOrganizer.Api.Users;

public sealed class RegisterUserRequest
{
    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;
}

public sealed record RegisterUserResponse(
    Guid UserId,
    string Email);
