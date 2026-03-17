namespace BIMConcierge.Api.Entities;

public class CompanyStandardEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string CompanyId { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Rule { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool AutoFix { get; set; }
    public int AlertLevel { get; set; } // 0=Info, 1=Warning, 2=Error

    public Company Company { get; set; } = null!;
}
