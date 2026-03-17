namespace BIMConcierge.Core.Models;

/// <summary>Represents a software license for the BIMConcierge plugin.</summary>
public class License
{
    public string Key        { get; set; } = string.Empty;
    public string CompanyId  { get; set; } = string.Empty;
    public int    MaxSeats   { get; set; }
    public int    UsedSeats  { get; set; }
    public LicenseType Type  { get; set; } = LicenseType.Professional;
    public DateTime ExpiresAt { get; set; }
    public bool IsValid => ExpiresAt > DateTime.UtcNow && UsedSeats < MaxSeats;
}

public enum LicenseType { Trial, Professional, Enterprise }
