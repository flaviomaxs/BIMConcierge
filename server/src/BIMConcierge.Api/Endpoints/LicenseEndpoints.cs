using BIMConcierge.Api.Data;
using BIMConcierge.Api.Dtos;
using Microsoft.EntityFrameworkCore;

namespace BIMConcierge.Api.Endpoints;

public static class LicenseEndpoints
{
    public static RouteGroupBuilder MapLicenseEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/validate/{key}", Validate);
        group.MapPost("/activate", Activate);
        return group;
    }

    private static async Task<IResult> Validate(string key, AppDbContext db)
    {
        var license = await db.Licenses.FirstOrDefaultAsync(l => l.Key == key);
        if (license is null)
            return Results.NotFound();

        return Results.Ok(new LicenseDto
        {
            Key = license.Key,
            CompanyId = license.CompanyId,
            MaxSeats = license.MaxSeats,
            UsedSeats = license.UsedSeats,
            Type = license.Type,
            ExpiresAt = license.ExpiresAt
        });
    }

    private static async Task<IResult> Activate(ActivateRequest req, AppDbContext db)
    {
        var license = await db.Licenses.FirstOrDefaultAsync(l => l.Key == req.LicenseKey);
        if (license is null)
            return Results.Ok(new ActivateResponse(false, "Chave de licença não encontrada."));

        if (license.ExpiresAt <= DateTime.UtcNow)
            return Results.Ok(new ActivateResponse(false, "Licença expirada."));

        if (license.UsedSeats >= license.MaxSeats)
            return Results.Ok(new ActivateResponse(false, "Limite de seats atingido."));

        license.UsedSeats++;
        await db.SaveChangesAsync();

        return Results.Ok(new ActivateResponse(true, null));
    }
}
