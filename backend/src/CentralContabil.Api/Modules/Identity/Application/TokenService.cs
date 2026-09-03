using CentralContabil.Api.Modules.Shared.Domain;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace CentralContabil.Api.Modules.Identity.Application;

public sealed record IssuedToken(string AccessToken, DateTimeOffset ExpiresAt, string RefreshToken);

public sealed class TokenService(IConfiguration config)
{
    public IssuedToken Issue(ApplicationUser user, IEnumerable<string> roles)
    {
        var now = DateTimeOffset.UtcNow;
        var expires = now.AddMinutes(config.GetValue("Jwt:AccessMinutes", 15));
        var claims = new List<Claim> {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()), new(JwtRegisteredClaimNames.Email, user.Email ?? ""),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()), new(ClaimTypes.Name, user.DisplayName) };
        claims.AddRange(roles.Select(x => new Claim(ClaimTypes.Role, x)));
        var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key())), SecurityAlgorithms.HmacSha256);
        var jwt = new JwtSecurityToken(config["Jwt:Issuer"], config["Jwt:Audience"], claims, now.UtcDateTime, expires.UtcDateTime, credentials);
        return new(new JwtSecurityTokenHandler().WriteToken(jwt), expires, Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)));
    }
    public string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    public string Key() => config["JWT_KEY"] ?? Environment.GetEnvironmentVariable("JWT_KEY")
        ?? throw new InvalidOperationException("JWT_KEY must be configured.");
}

