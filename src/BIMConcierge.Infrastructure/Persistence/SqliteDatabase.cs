using BIMConcierge.Core.Models;

namespace BIMConcierge.Infrastructure.Persistence;

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
}

/// <summary>
/// SQLite-backed local cache for offline use.
/// Full implementation wires up CreateTableAsync + CRUD per entity.
/// </summary>
public sealed class SqliteDatabase : ILocalDatabase
{
    public Task SaveUserAsync(User user)                                          => Task.CompletedTask;
    public Task<User?> GetLastUserAsync()                                         => Task.FromResult<User?>(null);
    public Task SaveTutorialsAsync(List<Tutorial> tutorials)                      => Task.CompletedTask;
    public Task<List<Tutorial>> GetTutorialsAsync(string? category = null)        => Task.FromResult(new List<Tutorial>());
    public Task<Tutorial?> GetTutorialAsync(string id)                            => Task.FromResult<Tutorial?>(null);
    public Task SaveProgressAsync(TutorialProgress progress)                      => Task.CompletedTask;
    public Task<TutorialProgress?> GetProgressAsync(string userId, string tutorialId) => Task.FromResult<TutorialProgress?>(null);
    public Task<List<TutorialProgress>> GetAllProgressAsync(string userId)        => Task.FromResult(new List<TutorialProgress>());
    public Task SaveStandardsAsync(List<CompanyStandard> standards)               => Task.CompletedTask;
    public Task<List<CompanyStandard>> GetStandardsAsync(string companyId)        => Task.FromResult(new List<CompanyStandard>());
}
