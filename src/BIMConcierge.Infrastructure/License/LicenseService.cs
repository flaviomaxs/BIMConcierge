using BIMConcierge.Core.Interfaces;
using BIMConcierge.Core.Models;
using BIMConcierge.Infrastructure.Api;

namespace BIMConcierge.Infrastructure.License;

public class LicenseService : ILicenseService
{
    private readonly IBimApiClient _api;

    public LicenseService(IBimApiClient api) => _api = api;

    public async Task<Core.Models.License?> ValidateAsync(string licenseKey)
    {
        try
        {
            return await _api.GetAsync<Core.Models.License>($"licenses/validate/{licenseKey}");
        }
        catch { return null; }
    }

    public async Task<bool> ActivateAsync(string licenseKey, string userId)
    {
        try
        {
            var result = await _api.PostAsync<ActivateRequest, ActivateResponse>(
                "licenses/activate",
                new ActivateRequest(licenseKey, userId));
            return result?.Success ?? false;
        }
        catch { return false; }
    }
}

internal sealed record ActivateRequest(string LicenseKey, string UserId);
internal sealed record ActivateResponse(bool Success, string? Message);
