using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BIMConcierge.Api.Entities;
using Microsoft.IdentityModel.Tokens;

namespace BIMConcierge.Api.Services;

public class AuthTokenService
{
    private readonly IConfiguration _config;

    public AuthTokenService(IConfiguration config) => _config = config;

    public string GenerateAccessToken(UserEntity user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            _config["Jwt:Secret"] ?? throw new InvalidOperationException("Jwt:Secret not configured")));

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("role", user.Role),
            new Claim("company_id", user.CompanyId),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"] ?? "BIMConcierge",
            audience: _config["Jwt:Audience"] ?? "BIMConcierge.Plugin",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(
                int.TryParse(_config["Jwt:AccessTokenMinutes"], out var mins) ? mins : 30),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var bytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }

    public int RefreshTokenDays =>
        int.TryParse(_config["Jwt:RefreshTokenDays"], out var d) ? d : 7;
}
