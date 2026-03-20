using BIMConcierge.Core.Models;

namespace BIMConcierge.Core.Interfaces;

public interface IProgressService
{
    Task<List<TutorialProgress>> GetUserProgressAsync(string userId);
    Task<List<Achievement>> GetAchievementsAsync(string userId);
    Task<List<LeaderboardEntry>> GetLeaderboardAsync(string companyId);
    Task UnlockAchievementAsync(string userId, string achievementId);
    Task AddXpAsync(string userId, int xp);
}
