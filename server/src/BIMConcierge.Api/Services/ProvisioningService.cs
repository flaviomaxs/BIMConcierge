using BIMConcierge.Api.Data;
using BIMConcierge.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace BIMConcierge.Api.Services;

public record ProvisioningResult(string LicenseKey, string UserId, string CompanyId);

public class ProvisioningService
{
    private readonly AppDbContext _db;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<ProvisioningService> _logger;

    public ProvisioningService(AppDbContext db, IEmailSender emailSender, ILogger<ProvisioningService> logger)
    {
        _db = db;
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task<ProvisioningResult> ProvisionAsync(
        string email, string name, string plan, int maxSeats, string paymentId)
    {
        // Check if user already exists
        var existingUser = await _db.Users
            .Include(u => u.Company)
            .FirstOrDefaultAsync(u => u.Email == email);

        string companyId;
        string userId;
        string? tempPassword = null;

        if (existingUser is not null)
        {
            companyId = existingUser.CompanyId;
            userId = existingUser.Id;
            _logger.LogInformation("User {Email} already exists — adding new license to company {CompanyId}", email, companyId);
        }
        else
        {
            // Create company
            var company = new Company
            {
                Name = $"Empresa de {name}"
            };
            _db.Companies.Add(company);
            companyId = company.Id;

            // Create user with temporary password
            tempPassword = GenerateTempPassword();
            var user = new UserEntity
            {
                Email = email,
                Name = name,
                PasswordHash = PasswordHasher.Hash(tempPassword),
                Role = "Admin",
                CompanyId = companyId
            };
            _db.Users.Add(user);
            userId = user.Id;

            _logger.LogInformation("Created new company {CompanyId} and user {UserId} for {Email}", companyId, userId, email);
        }

        // Generate license
        var (seats, durationDays) = plan.ToLowerInvariant() switch
        {
            "trial" => (1, 14),
            "enterprise" => (maxSeats > 0 ? maxSeats : 50, 365),
            _ => (maxSeats > 0 ? maxSeats : 5, 365) // Professional default
        };

        var license = new LicenseEntity
        {
            Key = LicenseKeyGenerator.Generate(),
            CompanyId = companyId,
            MaxSeats = seats,
            UsedSeats = 0,
            Type = plan,
            ExpiresAt = DateTime.UtcNow.AddDays(durationDays)
        };
        _db.Licenses.Add(license);

        // Update or create order
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.PaymentId == paymentId);
        if (order is not null)
        {
            order.Status = "Completed";
            order.LicenseId = license.Id;
            order.CompletedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        // Send welcome email (best-effort — don't fail provisioning if email fails)
        try
        {
            await _emailSender.SendWelcomeEmailAsync(email, name, license.Key, plan, tempPassword);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send welcome email for order {PaymentId} — license {Key} was still created", paymentId, license.Key);
        }

        return new ProvisioningResult(license.Key, userId, companyId);
    }

    private static string GenerateTempPassword()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789!@#$";
        var random = new Random();
        return new string(Enumerable.Range(0, 12).Select(_ => chars[random.Next(chars.Length)]).ToArray());
    }
}
