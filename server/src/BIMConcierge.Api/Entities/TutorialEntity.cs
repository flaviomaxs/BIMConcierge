namespace BIMConcierge.Api.Entities;

public class TutorialEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Difficulty { get; set; } = "Beginner";
    public int DurationMins { get; set; }
    public int StepCount { get; set; }
    public string ThumbnailUrl { get; set; } = string.Empty;
    public bool IsCompanyOwned { get; set; }

    public List<TutorialStepEntity> Steps { get; set; } = [];
}

public class TutorialStepEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string TutorialId { get; set; } = string.Empty;
    public int Order { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Instruction { get; set; } = string.Empty;
    public string? RevitCommand { get; set; }
    public string? HighlightZone { get; set; }
    public bool AutoApplicable { get; set; }
    public string? ValidationRule { get; set; }

    public TutorialEntity Tutorial { get; set; } = null!;
}
