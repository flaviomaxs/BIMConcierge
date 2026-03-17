using BIMConcierge.Api.Data;
using BIMConcierge.Api.Dtos;
using Microsoft.EntityFrameworkCore;

namespace BIMConcierge.Api.Endpoints;

public static class ProgressEndpoints
{
    public static RouteGroupBuilder MapProgressEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/progress/{userId}", GetProgress);
        group.MapGet("/achievements/{userId}", GetAchievements);
        group.MapPost("/achievements/{userId}/unlock/{achievementId}", UnlockAchievement);
        group.MapPost("/users/{userId}/xp", AddXp);
        return group;
    }

    private static async Task<IResult> GetProgress(string userId, AppDbContext db)
    {
        var progress = await db.Progress
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.StartedAt)
            .ToListAsync();

        return Results.Ok(progress.Select(p => new ProgressDto
        {
            UserId = p.UserId,
            TutorialId = p.TutorialId,
            CurrentStep = p.CurrentStep,
            TotalSteps = p.TotalSteps,
            IsCompleted = p.IsCompleted,
            ScorePercent = p.ScorePercent,
            StartedAt = p.StartedAt,
            CompletedAt = p.CompletedAt
        }).ToList());
    }

    private static async Task<IResult> GetAchievements(string userId, AppDbContext db)
    {
        var all = await db.Achievements.ToListAsync();
        var unlocked = await db.UserAchievements
            .Where(ua => ua.UserId == userId)
            .ToDictionaryAsync(ua => ua.AchievementId, ua => ua.UnlockedAt);

        return Results.Ok(all.Select(a => new AchievementDto
        {
            Id = a.Id,
            Title = a.Title,
            Description = a.Description,
            Icon = a.Icon,
            XpReward = a.XpReward,
            IsUnlocked = unlocked.ContainsKey(a.Id),
            UnlockedAt = unlocked.GetValueOrDefault(a.Id)
        }).ToList());
    }

    private static async Task<IResult> UnlockAchievement(string userId, string achievementId, AppDbContext db)
    {
        var exists = await db.UserAchievements
            .AnyAsync(ua => ua.UserId == userId && ua.AchievementId == achievementId);
        if (exists) return Results.Ok();

        db.UserAchievements.Add(new Entities.UserAchievement
        {
            UserId = userId,
            AchievementId = achievementId,
            UnlockedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        return Results.Ok();
    }

    private static async Task<IResult> AddXp(string userId, XpRequest req, AppDbContext db)
    {
        var user = await db.Users.FindAsync(userId);
        if (user is null) return Results.NotFound();

        user.XpPoints += req.Amount;
        // Simple leveling: every 500 XP = 1 level
        user.Level = 1 + user.XpPoints / 500;
        await db.SaveChangesAsync();
        return Results.Ok();
    }
}
