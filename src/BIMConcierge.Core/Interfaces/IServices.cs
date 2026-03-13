using BIMConcierge.Core.Models;

namespace BIMConcierge.Core.Interfaces;

public interface IAuthService
{
    bool IsAuthenticated { get; }
    User? CurrentUser { get; }
    Task<AuthResult> LoginAsync(string email, string password, string licenseKey);
    Task LogoutAsync();
    Task<bool> RefreshTokenAsync();
}

public interface ILicenseService
{
    Task<License?> ValidateAsync(string licenseKey);
    Task<bool> ActivateAsync(string licenseKey, string userId);
}

public interface ITutorialService
{
    Task<List<Tutorial>> GetAllAsync(string? category = null);
    Task<Tutorial?> GetByIdAsync(string id);
    Task<TutorialProgress?> GetProgressAsync(string userId, string tutorialId);
    Task SaveProgressAsync(TutorialProgress progress);
    Task<bool> CompleteStepAsync(string userId, string tutorialId, int stepIndex);
}

public interface IProgressService
{
    Task<List<TutorialProgress>> GetUserProgressAsync(string userId);
    Task<List<Achievement>> GetAchievementsAsync(string userId);
    Task UnlockAchievementAsync(string userId, string achievementId);
    Task AddXpAsync(string userId, int xp);
}

public interface IStandardsService
{
    Task<List<CompanyStandard>> GetStandardsAsync(string companyId);
    Task SaveStandardAsync(CompanyStandard standard);
    Task DeleteStandardAsync(string id);
    Task<List<CorrectionEvent>> ValidateModelAsync();
    Task<bool> AutoFixAsync(string correctionEventId);
}

public record AuthResult(bool Success, string? Token, string? ErrorMessage, User? User = null);
