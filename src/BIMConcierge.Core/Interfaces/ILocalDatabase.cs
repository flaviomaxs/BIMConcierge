using BIMConcierge.Core.Models;

namespace BIMConcierge.Core.Interfaces;

public interface ILocalDatabase
{
    Task SaveUserAsync(User user);
    Task<User?> GetLastUserAsync();
    Task SaveTutorialsAsync(List<Tutorial> tutorials);
    Task<List<Tutorial>> GetTutorialsAsync(string? category = null);
    Task<Tutorial?> GetTutorialAsync(string id);
    Task SaveProgressAsync(TutorialProgress progress);
    Task<TutorialProgress?> GetProgressAsync(string userId, string tutorialId);
    Task<List<TutorialProgress>> GetAllProgressAsync(string userId);
    Task SaveStandardsAsync(List<CompanyStandard> standards);
    Task<List<CompanyStandard>> GetStandardsAsync(string companyId);
    Task SaveLicenseAsync(License license);
    Task<License?> GetCachedLicenseAsync(string companyId);
}
