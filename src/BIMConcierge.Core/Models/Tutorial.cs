namespace BIMConcierge.Core.Models;

/// <summary>A guided tutorial available in the library.</summary>
public class Tutorial
{
    public string Id            { get; set; } = string.Empty;
    public string Title         { get; set; } = string.Empty;
    public string Description   { get; set; } = string.Empty;
    public string Category      { get; set; } = string.Empty;   // Walls, Families, MEP, Sheets…
    public string Difficulty    { get; set; } = "Beginner";      // Beginner | Intermediate | Advanced
    public int    DurationMins  { get; set; }
    public int    StepCount     { get; set; }
    public string ThumbnailUrl  { get; set; } = string.Empty;
    public bool   IsCompanyOwned { get; set; }
    public List<TutorialStep> Steps { get; set; } = [];
}

/// <summary>A single step inside a guided tutorial.</summary>
public class TutorialStep
{
    public int    Order          { get; set; }
    public string Title          { get; set; } = string.Empty;
    public string Instruction    { get; set; } = string.Empty;
    public string? RevitCommand  { get; set; }   // e.g. "ID_OBJECTS_WALL"
    public string? HighlightZone { get; set; }   // ribbon panel to highlight
    public bool   AutoApplicable { get; set; }   // can be applied automatically
    public string? ValidationRule { get; set; }  // expression to validate completion
}
