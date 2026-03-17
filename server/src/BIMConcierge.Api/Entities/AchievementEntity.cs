namespace BIMConcierge.Api.Entities;

public class AchievementEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public int XpReward { get; set; }

    public List<UserAchievement> UserAchievements { get; set; } = [];
}

public class UserAchievement
{
    public string UserId { get; set; } = string.Empty;
    public string AchievementId { get; set; } = string.Empty;
    public DateTime UnlockedAt { get; set; } = DateTime.UtcNow;

    public UserEntity User { get; set; } = null!;
    public AchievementEntity Achievement { get; set; } = null!;
}
