namespace BIMConcierge.Core.Models;

/// <summary>A company BIM standard rule that can be enforced in real time.</summary>
public class CompanyStandard
{
    public string Id          { get; set; } = string.Empty;
    public string CompanyId   { get; set; } = string.Empty;
    public string Category    { get; set; } = string.Empty;   // Naming | Levels | Families | Views…
    public string Name        { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Rule        { get; set; } = string.Empty;   // Regex or expression
    public bool   IsActive    { get; set; } = true;
    public bool   AutoFix     { get; set; }
    public Severity AlertLevel { get; set; } = Severity.Warning;
}
