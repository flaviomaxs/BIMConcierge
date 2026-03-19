namespace BIMConcierge.Api.Entities;

public class OrderEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Email { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Plan { get; set; } = "Professional"; // Trial | Professional | Enterprise
    public int MaxSeats { get; set; }
    public decimal PriceAmount { get; set; }
    public string Currency { get; set; } = "BRL";
    public string PaymentProvider { get; set; } = "Stripe";
    public string PaymentId { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending"; // Pending | Completed | Failed | Refunded
    public string? LicenseId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    public LicenseEntity? License { get; set; }
}
