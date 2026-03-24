namespace BIMConcierge.Api.Services;

public interface IEmailSender
{
    Task SendWelcomeEmailAsync(string toEmail, string customerName, string licenseKey, string plan, string? tempPassword);
}
