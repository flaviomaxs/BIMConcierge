using BIMConcierge.Core.Interfaces;
using BIMConcierge.Core.Models;
using BIMConcierge.Infrastructure.Api;
using Serilog;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BIMConcierge.Infrastructure.Auth;

/// <summary>
/// Authenticates users against the BIMConcierge cloud API.
/// Validates license, caches JWT + license locally for offline sessions.
/// </summary>
public class AuthService(IBimApiClient api, ITokenStore tokenStore, ILocalDatabase db, ILicenseService licenseService, IStringLocalizer loc) : IAuthService
{
    private readonly IBimApiClient _api = api;
    private readonly ITokenStore _tokenStore = tokenStore;
    private readonly ILocalDatabase _db = db;
    private readonly ILicenseService _licenseService = licenseService;
    private readonly IStringLocalizer _loc = loc;

    // Static so state survives across transient resolutions
    private static User? _currentUser;
    private static License? _currentLicense;
    private static string? _lastLicenseKey;
    private static readonly object _lock = new();
    private static event Action<bool>? _authStateChanged;

    public event Action<bool>? AuthStateChanged
    {
        add => _authStateChanged += value;
        remove => _authStateChanged -= value;
    }

    public bool IsAuthenticated => CurrentUser is not null
        && CurrentLicense is not null
        && !string.IsNullOrEmpty(_tokenStore.AccessToken);

    public User? CurrentUser
    {
        get { lock (_lock) return _currentUser; }
        private set { lock (_lock) _currentUser = value; }
    }

    public License? CurrentLicense
    {
        get { lock (_lock) return _currentLicense; }
        private set { lock (_lock) _currentLicense = value; }
    }

    public async Task<AuthResult> LoginAsync(string email, string password, string licenseKey, CancellationToken ct = default)
    {
        // Dev login — bypass API for local testing (only when env var is set)
        if (Environment.GetEnvironmentVariable("BIMCONCIERGE_DEV_MODE") == "true"
            && email.Trim().Equals("dev@bimconcierge.com", StringComparison.OrdinalIgnoreCase)
            && password.Trim() == "dev")
        {
            return await DevLoginAsync(licenseKey);
        }

        try
        {
            LoginResponse? response = await _api.PostAsync<LoginRequest, LoginResponse>(
                "auth/login",
                new LoginRequest(email, password, licenseKey), ct).ConfigureAwait(false);

            if (response is null || !response.Success)
                return new AuthResult(false, null, response?.Message ?? _loc.GetString("AuthInvalidCredentials"));

            _tokenStore.AccessToken = response.AccessToken;
            _tokenStore.RefreshToken = response.RefreshToken;
            CurrentUser = response.User;

            // Validate license
            License? license = await _licenseService.ValidateAsync(licenseKey);
            AuthResult? licenseResult = EnforceLicense(license);
            if (licenseResult is not null) return licenseResult;

            CurrentLicense = license;
            _lastLicenseKey = licenseKey;

            // Cache for offline use
            await _db.SaveUserAsync(CurrentUser);
            await _db.SaveLicenseAsync(license!);

            _authStateChanged?.Invoke(true);
            return new AuthResult(true, response.AccessToken, null, CurrentUser, CurrentLicense);
        }
        catch (HttpRequestException ex)
        {
            Log.Warning(ex, "API unreachable during login — attempting offline fallback");
            return await OfflineFallbackAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unexpected error during login");
            return new AuthResult(false, null, ex.Message);
        }
    }

    public async Task<bool> EnsureValidSessionAsync()
    {
        if (CurrentUser is null || CurrentLicense is null)
            return false;

        // Check license expiration
        if (!CurrentLicense.IsValid)
        {
            Log.Information("License expired or seats exhausted — forcing re-login");
            await LogoutAsync();
            return false;
        }

        // Check JWT expiration and try refresh
        if (IsTokenExpired(_tokenStore.AccessToken))
        {
            Log.Information("Access token expired — attempting refresh");
            if (!await RefreshTokenAsync())
            {
                Log.Warning("Token refresh failed — forcing re-login");
                await LogoutAsync();
                return false;
            }
        }

        // Periodically revalidate license (if we have a key and API is reachable)
        if (_lastLicenseKey is not null)
        {
            try
            {
                License? license = await _licenseService.ValidateAsync(_lastLicenseKey);
                if (license is not null)
                {
                    CurrentLicense = license;
                    await _db.SaveLicenseAsync(license);

                    if (!license.IsValid)
                    {
                        Log.Warning("License no longer valid after revalidation");
                        await LogoutAsync();
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "License revalidation failed — using cached license");
            }
        }

        return true;
    }

    public async Task LogoutAsync()
    {
        CurrentUser = null;
        CurrentLicense = null;
        _lastLicenseKey = null;
        _tokenStore.AccessToken = null;
        _tokenStore.RefreshToken = null;
        _authStateChanged?.Invoke(false);
        await Task.CompletedTask;
    }

    public async Task<bool> RefreshTokenAsync()
    {
        if (string.IsNullOrEmpty(_tokenStore.RefreshToken)) return false;
        try
        {
            LoginResponse? response = await _api.PostAsync<RefreshRequest, LoginResponse>(
                "auth/refresh",
                new RefreshRequest(_tokenStore.RefreshToken));

            if (response is null || !response.Success) return false;

            _tokenStore.AccessToken = response.AccessToken;
            _tokenStore.RefreshToken = response.RefreshToken;
            return true;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Token refresh failed");
            return false;
        }
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private async Task<AuthResult> DevLoginAsync(string licenseKey)
    {
        var devUser = new User
        {
            Id = "dev-001",
            Name = "Dev User",
            Email = "dev@bimconcierge.com",
            Role = "Admin",
            CompanyId = "dev-company",
            XpPoints = 2500,
            Level = 12
        };
        var devLicense = new License
        {
            Key = string.IsNullOrWhiteSpace(licenseKey) ? "DEV-LICENSE" : licenseKey,
            CompanyId = "dev-company",
            MaxSeats = 999,
            UsedSeats = 1,
            Type = LicenseType.Enterprise,
            ExpiresAt = DateTime.UtcNow.AddYears(1)
        };

        _tokenStore.AccessToken = "dev-token";
        _tokenStore.RefreshToken = "dev-refresh";
        CurrentUser = devUser;
        CurrentLicense = devLicense;
        _lastLicenseKey = devLicense.Key;

        try
        {
            await _db.SaveUserAsync(devUser);
            await _db.SaveLicenseAsync(devLicense);
        }
        catch (Exception ex) { Log.Warning(ex, "Failed to cache dev data — continuing"); }

        Log.Information("Dev login activated — bypassing API");
        _authStateChanged?.Invoke(true);
        return new AuthResult(true, "dev-token", null, devUser, devLicense);
    }

    private async Task<AuthResult> OfflineFallbackAsync()
    {
        User? cached = await _db.GetLastUserAsync();
        if (cached is null)
            return new AuthResult(false, null, _loc.GetString("AuthNoConnectionNoUser"));

        License? cachedLicense = await _db.GetCachedLicenseAsync(cached.CompanyId);
        if (cachedLicense is null)
            return new AuthResult(false, null, _loc.GetString("AuthNoConnectionNoLicense"));

        if (!cachedLicense.IsValid)
            return new AuthResult(false, null, cachedLicense.ExpiresAt <= DateTime.UtcNow
                ? _loc.GetString("AuthLicenseExpiredRenew")
                : _loc.GetString("AuthSeatLimitReached"));

        CurrentUser = cached;
        CurrentLicense = cachedLicense;
        _authStateChanged?.Invoke(true);
        return new AuthResult(true, _tokenStore.AccessToken, _loc.GetString("AuthOfflineMode"), cached, cachedLicense);
    }

    /// <summary>
    /// Returns an error AuthResult if the license is invalid, or null if valid.
    /// </summary>
    private AuthResult? EnforceLicense(License? license)
    {
        if (license is null)
            return new AuthResult(false, null, _loc.GetString("AuthInvalidLicenseKey"));

        if (license.ExpiresAt <= DateTime.UtcNow)
            return new AuthResult(false, null, _loc.GetString("AuthLicenseExpired"));

        if (license.UsedSeats >= license.MaxSeats)
            return new AuthResult(false, null, _loc.GetString("AuthSeatLimitReached"));

        return null;
    }

    /// <summary>
    /// Decodes the JWT exp claim to check if the token is expired.
    /// Returns true if expired or if the token cannot be parsed.
    /// </summary>
    private static bool IsTokenExpired(string? token)
    {
        if (string.IsNullOrEmpty(token)) return true;

        try
        {
            var parts = token.Split('.');
            if (parts.Length != 3) return true;

            var payload = parts[1];
            // Pad base64 if needed
            payload = payload.Replace('-', '+').Replace('_', '/');
            switch (payload.Length % 4)
            {
                case 2: payload += "=="; break;
                case 3: payload += "="; break;
            }

            var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("exp", out JsonElement expProp))
            {
                var exp = DateTimeOffset.FromUnixTimeSeconds(expProp.GetInt64());
                return exp <= DateTimeOffset.UtcNow;
            }
            return true;
        }
        catch
        {
            return true;
        }
    }
}

// ── DTOs ────────────────────────────────────────────────────────────────────
internal sealed record LoginRequest(string Email, string Password, string LicenseKey);
internal sealed record RefreshRequest(string RefreshToken);
internal sealed record LoginResponse(bool Success, string? Message,
    string AccessToken, string RefreshToken, User User);
