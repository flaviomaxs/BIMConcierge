using BIMConcierge.Core.Interfaces;
using BIMConcierge.Core.Models;
using BIMConcierge.Infrastructure.Api;
using BIMConcierge.Infrastructure.Persistence;

namespace BIMConcierge.Infrastructure.Api;

// ── Tutorial Service ─────────────────────────────────────────────────────────

public class TutorialService : ITutorialService
{
    private readonly IBimApiClient  _api;
    private readonly ILocalDatabase _db;

    public TutorialService(IBimApiClient api, ILocalDatabase db) { _api = api; _db = db; }

    public async Task<List<Tutorial>> GetAllAsync(string? category = null)
    {
        try
        {
            var url  = string.IsNullOrEmpty(category) ? "tutorials" : $"tutorials?category={category}";
            var list = await _api.GetAsync<List<Tutorial>>(url);
            if (list is not null) await _db.SaveTutorialsAsync(list);
            return list ?? [];
        }
        catch { return await _db.GetTutorialsAsync(category); }
    }

    public async Task<Tutorial?> GetByIdAsync(string id)
    {
        try   { return await _api.GetAsync<Tutorial>($"tutorials/{id}"); }
        catch { return await _db.GetTutorialAsync(id); }
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

// ── Progress / Achievements Service ─────────────────────────────────────────

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
        catch { return await _db.GetAllProgressAsync(userId); }
    }

    public async Task<List<Achievement>> GetAchievementsAsync(string userId)
    {
        try   { return await _api.GetAsync<List<Achievement>>($"achievements/{userId}") ?? []; }
        catch { return []; }
    }

    public async Task UnlockAchievementAsync(string userId, string achievementId) =>
        await _api.PostAsync<object, object>(
            $"achievements/{userId}/unlock/{achievementId}", new { });

    public async Task AddXpAsync(string userId, int xp) =>
        await _api.PostAsync<object, object>($"users/{userId}/xp", new { Amount = xp });
}

// ── Standards Service ────────────────────────────────────────────────────────

public class StandardsService : IStandardsService
{
    private readonly IBimApiClient  _api;
    private readonly ILocalDatabase _db;

    public StandardsService(IBimApiClient api, ILocalDatabase db) { _api = api; _db = db; }

    public async Task<List<CompanyStandard>> GetStandardsAsync(string companyId)
    {
        try
        {
            var list = await _api.GetAsync<List<CompanyStandard>>($"standards/{companyId}");
            if (list is not null) await _db.SaveStandardsAsync(list);
            return list ?? [];
        }
        catch { return await _db.GetStandardsAsync(companyId); }
    }

    public async Task SaveStandardAsync(CompanyStandard standard) =>
        await _api.PutAsync<CompanyStandard, object>($"standards/{standard.Id}", standard);

    public async Task DeleteStandardAsync(string id) =>
        await _api.DeleteAsync($"standards/{id}");

    /// <summary>
    /// Validates the active Revit document against company standards.
    /// Real implementation hooks into the RevitEventDispatcher.
    /// </summary>
    public Task<List<CorrectionEvent>> ValidateModelAsync() =>
        Task.FromResult(new List<CorrectionEvent>());   // wired up in RevitEventDispatcher

    public Task<bool> AutoFixAsync(string correctionEventId) =>
        Task.FromResult(false); // wired up in RevitEventDispatcher
}
