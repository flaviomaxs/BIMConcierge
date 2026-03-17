using BIMConcierge.Core.Interfaces;
using BIMConcierge.Core.Models;
using Serilog;

namespace BIMConcierge.Infrastructure.Api;

public class TutorialService : ITutorialService
{
    private readonly IBimApiClient  _api;
    private readonly ILocalDatabase _db;

    public TutorialService(IBimApiClient api, ILocalDatabase db) { _api = api; _db = db; }

    public async Task<List<Tutorial>> GetAllAsync(string? category = null)
    {
        try
        {
            var url = string.IsNullOrEmpty(category)
                ? "tutorials"
                : $"tutorials?category={Uri.EscapeDataString(category)}";
            var list = await _api.GetAsync<List<Tutorial>>(url);
            if (list is not null) await _db.SaveTutorialsAsync(list);
            return list ?? [];
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "API call failed for tutorials — falling back to local cache");
            return await _db.GetTutorialsAsync(category);
        }
    }

    public async Task<Tutorial?> GetByIdAsync(string id)
    {
        try   { return await _api.GetAsync<Tutorial>($"tutorials/{id}"); }
        catch (Exception ex)
        {
            Log.Warning(ex, "API call failed for tutorial {Id} — falling back to local cache", id);
            return await _db.GetTutorialAsync(id);
        }
    }

    public async Task<TutorialProgress?> GetProgressAsync(string userId, string tutorialId) =>
        await _db.GetProgressAsync(userId, tutorialId);

    public async Task SaveProgressAsync(TutorialProgress progress) =>
        await _db.SaveProgressAsync(progress);

    public async Task<bool> CompleteStepAsync(string userId, string tutorialId, int stepIndex)
    {
        var progress = await _db.GetProgressAsync(userId, tutorialId)
                       ?? new TutorialProgress { UserId = userId, TutorialId = tutorialId, StartedAt = DateTime.UtcNow };

        progress.CurrentStep = Math.Max(progress.CurrentStep, stepIndex + 1);
        if (progress.CurrentStep >= progress.TotalSteps)
        {
            progress.IsCompleted  = true;
            progress.CompletedAt  = DateTime.UtcNow;
            progress.ScorePercent = 100;
        }

        await _db.SaveProgressAsync(progress);
        return true;
    }
}
