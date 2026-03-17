namespace BIMConcierge.Api.Entities;

public class LicenseEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Key { get; set; } = string.Empty;
    public string CompanyId { get; set; } = string.Empty;
    public int MaxSeats { get; set; }
    public int UsedSeats { get; set; }
    public string Type { get; set; } = "Professional"; // Trial | Professional | Enterprise
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Company Company { get; set; } = null!;
}
