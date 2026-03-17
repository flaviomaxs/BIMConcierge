using BIMConcierge.Api.Data;
using BIMConcierge.Api.Dtos;
using Microsoft.EntityFrameworkCore;

namespace BIMConcierge.Api.Endpoints;

public static class StandardsEndpoints
{
    public static RouteGroupBuilder MapStandardsEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/{companyId}", GetByCompany);
        group.MapPut("/{id}", Upsert);
        group.MapDelete("/{id}", Delete);
        return group;
    }

    private static async Task<IResult> GetByCompany(string companyId, AppDbContext db)
    {
        var standards = await db.CompanyStandards
            .Where(s => s.CompanyId == companyId)
            .OrderBy(s => s.Category).ThenBy(s => s.Name)
            .ToListAsync();

        return Results.Ok(standards.Select(s => new CompanyStandardDto
        {
            Id = s.Id,
            CompanyId = s.CompanyId,
            Category = s.Category,
            Name = s.Name,
            Description = s.Description,
            Rule = s.Rule,
            IsActive = s.IsActive,
            AutoFix = s.AutoFix,
            AlertLevel = s.AlertLevel
        }).ToList());
    }

    private static async Task<IResult> Upsert(string id, CompanyStandardDto dto, AppDbContext db)
    {
        var existing = await db.CompanyStandards.FindAsync(id);
        if (existing is not null)
        {
            existing.Category = dto.Category;
            existing.Name = dto.Name;
            existing.Description = dto.Description;
            existing.Rule = dto.Rule;
            existing.IsActive = dto.IsActive;
            existing.AutoFix = dto.AutoFix;
            existing.AlertLevel = dto.AlertLevel;
        }
        else
        {
            db.CompanyStandards.Add(new Entities.CompanyStandardEntity
            {
                Id = id,
                CompanyId = dto.CompanyId,
                Category = dto.Category,
                Name = dto.Name,
                Description = dto.Description,
                Rule = dto.Rule,
                IsActive = dto.IsActive,
                AutoFix = dto.AutoFix,
                AlertLevel = dto.AlertLevel
            });
        }

        await db.SaveChangesAsync();
        return Results.Ok();
    }

    private static async Task<IResult> Delete(string id, AppDbContext db)
    {
        var standard = await db.CompanyStandards.FindAsync(id);
        if (standard is null) return Results.NotFound();

        db.CompanyStandards.Remove(standard);
        await db.SaveChangesAsync();
        return Results.Ok();
    }
}
