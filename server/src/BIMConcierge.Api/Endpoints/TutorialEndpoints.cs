using BIMConcierge.Api.Data;
using BIMConcierge.Api.Dtos;
using Microsoft.EntityFrameworkCore;

namespace BIMConcierge.Api.Endpoints;

public static class TutorialEndpoints
{
    public static RouteGroupBuilder MapTutorialEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", GetAll);
        group.MapGet("/{id}", GetById);
        return group;
    }

    private static async Task<IResult> GetAll(AppDbContext db, string? category = null)
    {
        var query = db.Tutorials.Include(t => t.Steps).AsQueryable();
        if (!string.IsNullOrEmpty(category))
            query = query.Where(t => t.Category == category);

        var tutorials = await query.OrderBy(t => t.Title).ToListAsync();
        return Results.Ok(tutorials.Select(MapTutorial).ToList());
    }

    private static async Task<IResult> GetById(string id, AppDbContext db)
    {
        var tutorial = await db.Tutorials.Include(t => t.Steps).FirstOrDefaultAsync(t => t.Id == id);
        if (tutorial is null)
            return Results.NotFound();

        return Results.Ok(MapTutorial(tutorial));
    }

    private static TutorialDto MapTutorial(Entities.TutorialEntity t) => new()
    {
        Id = t.Id,
        Title = t.Title,
        Description = t.Description,
        Category = t.Category,
        Difficulty = t.Difficulty,
        DurationMins = t.DurationMins,
        StepCount = t.StepCount,
        ThumbnailUrl = t.ThumbnailUrl,
        IsCompanyOwned = t.IsCompanyOwned,
        Steps = t.Steps.OrderBy(s => s.Order).Select(s => new TutorialStepDto
        {
            Order = s.Order,
            Title = s.Title,
            Instruction = s.Instruction,
            RevitCommand = s.RevitCommand,
            HighlightZone = s.HighlightZone,
            AutoApplicable = s.AutoApplicable,
            ValidationRule = s.ValidationRule
        }).ToList()
    };
}
