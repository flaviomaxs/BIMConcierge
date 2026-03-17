namespace BIMConcierge.Api.Entities;

public class Company
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<UserEntity> Users { get; set; } = [];
    public List<LicenseEntity> Licenses { get; set; } = [];
    public List<CompanyStandardEntity> Standards { get; set; } = [];
}
