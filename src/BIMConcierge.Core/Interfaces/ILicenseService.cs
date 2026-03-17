using BIMConcierge.Core.Models;

namespace BIMConcierge.Core.Interfaces;

public interface ILicenseService
{
    Task<License?> ValidateAsync(string licenseKey);
    Task<bool> ActivateAsync(string licenseKey, string userId);
}
