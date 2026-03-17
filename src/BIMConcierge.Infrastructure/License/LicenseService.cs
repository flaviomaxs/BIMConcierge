using BIMConcierge.Core.Interfaces;
using BIMConcierge.Core.Models;
using BIMConcierge.Infrastructure.Api;
using Serilog;

namespace BIMConcierge.Infrastructure.Licensing;

public class LicenseService : ILicenseService
{
    private readonly IBimApiClient _api;

    public LicenseService(IBimApiClient api) => _api = api;

    public async Task<License?> ValidateAsync(string licenseKey)
    {
        try
        {
            return await _api.GetAsync<License>($"licenses/validate/{licenseKey}");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "License validation failed for key {Key}", licenseKey[..Math.Min(8, licenseKey.Length)] + "***");
            return null;
        }
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
        catch (Exception ex)
        {
            Log.Error(ex, "License activation failed");
            return false;
        }
    }
}

internal sealed record ActivateRequest(string LicenseKey, string UserId);
internal sealed record ActivateResponse(bool Success, string? Message);
