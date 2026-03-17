using BIMConcierge.Core.Models;

namespace BIMConcierge.Core.Interfaces;

public interface IAuthService
{
    bool IsAuthenticated { get; }
    User? CurrentUser { get; }
    License? CurrentLicense { get; }
    Task<AuthResult> LoginAsync(string email, string password, string licenseKey);
    Task LogoutAsync();
    Task<bool> RefreshTokenAsync();
    Task<bool> EnsureValidSessionAsync();
}

public record AuthResult(bool Success, string? Token, string? ErrorMessage, User? User = null, License? License = null);
