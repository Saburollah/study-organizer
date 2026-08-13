using Microsoft.AspNetCore.Identity;
using StudyOrganizer.Domain.Users;

namespace StudyOrganizer.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public ApplicationUser()
    {
        Id = Guid.NewGuid();
    }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public ProfileGender? Gender { get; set; }
}
