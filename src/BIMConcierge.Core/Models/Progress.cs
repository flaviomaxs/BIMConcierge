namespace BIMConcierge.Core.Models;

/// <summary>Tracks a user's progress through a specific tutorial.</summary>
public class TutorialProgress
{
    public string   UserId       { get; set; } = string.Empty;
    public string   TutorialId   { get; set; } = string.Empty;
    public int      CurrentStep  { get; set; }
    public int      TotalSteps   { get; set; }
    public bool     IsCompleted  { get; set; }
    public int      ScorePercent { get; set; }
    public DateTime StartedAt    { get; set; }
    public DateTime? CompletedAt { get; set; }
    public double ProgressPercent => TotalSteps == 0 ? 0 : (double)CurrentStep / TotalSteps * 100;
}

/// <summary>Gamification achievement / badge.</summary>
public class Achievement
{
    public string Id          { get; set; } = string.Empty;
    public string Title       { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon        { get; set; } = string.Empty;  // Material symbol name
    public int    XpReward    { get; set; }
    public bool   IsUnlocked  { get; set; }
    public DateTime? UnlockedAt { get; set; }
}

/// <summary>A real-time correction event raised by the Revit watcher.</summary>
public class CorrectionEvent
{
    public string   Id          { get; set; } = Guid.NewGuid().ToString();
    public string   RuleId      { get; set; } = string.Empty;
    public string   Title       { get; set; } = string.Empty;
    public string   Description { get; set; } = string.Empty;
    public Severity Severity    { get; set; } = Severity.Warning;
    public bool     CanAutoFix  { get; set; }
    public string?  ElementId   { get; set; }
    public DateTime OccurredAt  { get; set; } = DateTime.Now;
    public bool     IsFixed     { get; set; }
}

public enum Severity { Info, Warning, Error }

/// <summary>Represents a leaderboard entry for company ranking.</summary>
public class LeaderboardEntry
{
    public int    Rank          { get; set; }
    public string Name          { get; set; } = string.Empty;
    public string Title         { get; set; } = string.Empty;
    public int    XpPoints      { get; set; }
    public bool   IsCurrentUser { get; set; }
}
