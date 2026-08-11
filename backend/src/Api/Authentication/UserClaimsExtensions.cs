using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace StudyOrganizer.Api.Authentication;

public static class UserClaimsExtensions
{
    public static bool TryGetUserId(
        this ClaimsPrincipal principal,
        out Guid userId)
    {
        var value =
            principal.FindFirstValue(
                JwtRegisteredClaimNames.Sub)
            ?? principal.FindFirstValue(
                ClaimTypes.NameIdentifier);

        return Guid.TryParse(
            value,
            out userId);
    }
}
