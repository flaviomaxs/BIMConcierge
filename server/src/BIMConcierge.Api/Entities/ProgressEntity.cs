namespace BIMConcierge.Api.Entities;

public class ProgressEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string TutorialId { get; set; } = string.Empty;
    public int CurrentStep { get; set; }
    public int TotalSteps { get; set; }
    public bool IsCompleted { get; set; }
    public int ScorePercent { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    public UserEntity User { get; set; } = null!;
    public TutorialEntity Tutorial { get; set; } = null!;
}
