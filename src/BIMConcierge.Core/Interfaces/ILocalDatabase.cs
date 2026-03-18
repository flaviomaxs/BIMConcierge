using BIMConcierge.Core.Models;

namespace BIMConcierge.Core.Interfaces;

public interface ILocalDatabase : IDisposable
{
    // ── User ────────────────────────────────────────────────────────────────
    Task SaveUserAsync(User user);
    Task<User?> GetLastUserAsync();
    Task<User?> GetUserAsync(string id);
    Task DeleteUserAsync(string id);

    // ── Tutorials ───────────────────────────────────────────────────────────
    Task SaveTutorialsAsync(List<Tutorial> tutorials);
    Task SaveTutorialAsync(Tutorial tutorial);
    Task<List<Tutorial>> GetTutorialsAsync(string? category = null);
    Task<Tutorial?> GetTutorialAsync(string id);
    Task DeleteTutorialAsync(string id);

    // ── Progress ────────────────────────────────────────────────────────────
    Task SaveProgressAsync(TutorialProgress progress);
    Task<TutorialProgress?> GetProgressAsync(string userId, string tutorialId);
    Task<List<TutorialProgress>> GetAllProgressAsync(string userId);
    Task DeleteProgressAsync(string userId, string tutorialId);

    // ── Standards ───────────────────────────────────────────────────────────
    Task SaveStandardsAsync(List<CompanyStandard> standards);
    Task SaveStandardAsync(CompanyStandard standard);
    Task<List<CompanyStandard>> GetStandardsAsync(string companyId);
    Task<CompanyStandard?> GetStandardByIdAsync(string id);
    Task DeleteStandardAsync(string id);

    // ── Achievements ────────────────────────────────────────────────────────
    Task SaveAchievementsAsync(List<Achievement> achievements);
    Task<List<Achievement>> GetAchievementsAsync(string userId);
    Task SaveAchievementAsync(Achievement achievement, string userId);

    // ── License ─────────────────────────────────────────────────────────────
    Task SaveLicenseAsync(License license);
    Task<License?> GetCachedLicenseAsync(string companyId);
    Task DeleteLicenseAsync(string companyId);

    // ── Maintenance ─────────────────────────────────────────────────────────
    Task ClearAllAsync();
    Task<long> GetDatabaseSizeAsync();
}
