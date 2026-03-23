namespace BIMConcierge.Core.Models;

/// <summary>Represents an authenticated BIMConcierge user.</summary>
public class User
{
    public string Id          { get; set; } = string.Empty;
    public string Name        { get; set; } = string.Empty;
    public string Email       { get; set; } = string.Empty;
    public string Role        { get; set; } = "Collaborator";   // Admin | Manager | Collaborator
    public string CompanyId   { get; set; } = string.Empty;
    public string AvatarUrl   { get; set; } = string.Empty;
    public int    XpPoints    { get; set; }
    public int    Level       { get; set; } = 1;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Computed initials from the user's name (e.g. "FF" for "Flávio Filho").</summary>
    public string Initials => string.IsNullOrWhiteSpace(Name)
        ? "?"
        : string.Concat(Name.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Take(2).Select(w => char.ToUpperInvariant(w[0])));
}
