using BIMConcierge.Core.Interfaces;
using BIMConcierge.Core.Models;
using Serilog;

namespace BIMConcierge.Infrastructure.Api;

public class ProgressService : IProgressService
{
    private readonly IBimApiClient  _api;
    private readonly ILocalDatabase _db;

    public ProgressService(IBimApiClient api, ILocalDatabase db) { _api = api; _db = db; }

    public async Task<List<TutorialProgress>> GetUserProgressAsync(string userId)
    {
        try
        {
            var list = await _api.GetAsync<List<TutorialProgress>>($"progress/{userId}");
            return list ?? [];
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "API call failed for progress — falling back to local cache");
            return await _db.GetAllProgressAsync(userId);
        }
    }

    public async Task<List<Achievement>> GetAchievementsAsync(string userId)
    {
        try   { return await _api.GetAsync<List<Achievement>>($"achievements/{userId}") ?? []; }
        catch (Exception ex)
        {
            Log.Warning(ex, "API call failed for achievements");
            return [];
        }
    }

    public async Task UnlockAchievementAsync(string userId, string achievementId) =>
        await _api.PostAsync<object, object>(
            $"achievements/{userId}/unlock/{achievementId}", new { });

    public async Task AddXpAsync(string userId, int xp) =>
        await _api.PostAsync<object, object>($"users/{userId}/xp", new { Amount = xp });
}
