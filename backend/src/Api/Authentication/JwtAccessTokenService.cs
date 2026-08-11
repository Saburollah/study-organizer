using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using StudyOrganizer.Application.Authentication;

namespace StudyOrganizer.Api.Authentication;

public sealed class JwtAccessTokenService(
    JwtOptions options,
    TimeProvider timeProvider)
    : IAccessTokenService
{
    public AccessTokenResult Create(
        Guid userId,
        string email)
    {
        var issuedAt = timeProvider.GetUtcNow();
        var expiresAt = issuedAt.AddMinutes(
            options.ExpiresInMinutes);

        var claims = new[]
        {
            new Claim(
                JwtRegisteredClaimNames.Sub,
                userId.ToString()),
            new Claim(
                JwtRegisteredClaimNames.Email,
                email),
            new Claim(
                JwtRegisteredClaimNames.Jti,
                Guid.NewGuid().ToString())
        };

        var signingKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(
                options.SigningKey));

        var signingCredentials = new SigningCredentials(
            signingKey,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            notBefore: issuedAt.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: signingCredentials);

        var value =
            new JwtSecurityTokenHandler()
                .WriteToken(token);

        return new AccessTokenResult(
            value,
            expiresAt);
    }
}
