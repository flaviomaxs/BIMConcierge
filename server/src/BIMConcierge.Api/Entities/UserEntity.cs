namespace BIMConcierge.Api.Entities;

public class UserEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = "Collaborator"; // Admin | Manager | Collaborator
    public string CompanyId { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public int XpPoints { get; set; }
    public int Level { get; set; } = 1;
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Company Company { get; set; } = null!;
    public List<ProgressEntity> Progress { get; set; } = [];
    public List<UserAchievement> Achievements { get; set; } = [];
}
