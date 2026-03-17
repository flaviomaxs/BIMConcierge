using BIMConcierge.Api.Data;
using BIMConcierge.Api.Dtos;
using BIMConcierge.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace BIMConcierge.Api.Endpoints;

public static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuthEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/login", Login).AllowAnonymous();
        group.MapPost("/refresh", Refresh).AllowAnonymous();
        return group;
    }

    private static async Task<IResult> Login(LoginRequest req, AppDbContext db, AuthTokenService tokens)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == req.Email);
        if (user is null || !PasswordHasher.Verify(req.Password, user.PasswordHash))
            return Results.Ok(new LoginResponse(false, "Credenciais inválidas.", "", "", null!));

        // Validate license
        var license = await db.Licenses.FirstOrDefaultAsync(l => l.Key == req.LicenseKey);
        if (license is null)
            return Results.Ok(new LoginResponse(false, "Chave de licença inválida.", "", "", null!));

        if (license.ExpiresAt <= DateTime.UtcNow)
            return Results.Ok(new LoginResponse(false, "Licença expirada. Contate o administrador.", "", "", null!));

        if (license.UsedSeats >= license.MaxSeats)
            return Results.Ok(new LoginResponse(false, "Limite de seats atingido. Contate o administrador.", "", "", null!));

        // Verify user belongs to the license's company
        if (user.CompanyId != license.CompanyId)
            return Results.Ok(new LoginResponse(false, "Licença não pertence à sua empresa.", "", "", null!));

        // Generate tokens
        var accessToken = tokens.GenerateAccessToken(user);
        var refreshToken = tokens.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(tokens.RefreshTokenDays);
        await db.SaveChangesAsync();

        var userDto = MapUser(user);
        return Results.Ok(new LoginResponse(true, null, accessToken, refreshToken, userDto));
    }

    private static async Task<IResult> Refresh(RefreshRequest req, AppDbContext db, AuthTokenService tokens)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.RefreshToken == req.RefreshToken);
        if (user is null || user.RefreshTokenExpiresAt <= DateTime.UtcNow)
            return Results.Ok(new LoginResponse(false, "Refresh token inválido ou expirado.", "", "", null!));

        var accessToken = tokens.GenerateAccessToken(user);
        var refreshToken = tokens.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(tokens.RefreshTokenDays);
        await db.SaveChangesAsync();

        return Results.Ok(new LoginResponse(true, null, accessToken, refreshToken, MapUser(user)));
    }

    private static UserDto MapUser(Entities.UserEntity u) => new()
    {
        Id = u.Id,
        Name = u.Name,
        Email = u.Email,
        Role = u.Role,
        CompanyId = u.CompanyId,
        AvatarUrl = u.AvatarUrl,
        XpPoints = u.XpPoints,
        Level = u.Level,
        CreatedAt = u.CreatedAt
    };
}
