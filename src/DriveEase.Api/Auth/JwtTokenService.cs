using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DriveEase.Shared;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace DriveEase.Api.Auth;

public sealed class JwtTokenService(IOptions<JwtOptions> options, IClock clock)
{
    public string GenerateAccessToken(Guid userId, string email, string fullName, string role = "Student")
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Value.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(JwtRegisteredClaimNames.Name, fullName),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: options.Value.Issuer,
            audience: options.Value.Audience,
            claims: claims,
            expires: clock.UtcNow.UtcDateTime.AddMinutes(options.Value.ExpiryMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
