using BIMConcierge.Core.Interfaces;
using BIMConcierge.Core.Models;
using BIMConcierge.Infrastructure.Api;
using BIMConcierge.Infrastructure.Persistence;

namespace BIMConcierge.Infrastructure.Auth;

/// <summary>
/// Authenticates users against the BIM Concierge cloud API.
/// Caches the JWT token locally for offline sessions.
/// </summary>
public class AuthService : IAuthService
{
    private readonly IBimApiClient _api;
    private readonly ITokenStore   _tokenStore;
    private readonly ILocalDatabase _db;

    public bool  IsAuthenticated => CurrentUser is not null && !string.IsNullOrEmpty(_tokenStore.AccessToken);
    public User? CurrentUser { get; private set; }

    public AuthService(IBimApiClient api, ITokenStore tokenStore, ILocalDatabase db)
    {
        _api        = api;
        _tokenStore = tokenStore;
        _db         = db;
    }

    public async Task<AuthResult> LoginAsync(string email, string password, string licenseKey)
    {
        try
        {
            var response = await _api.PostAsync<LoginRequest, LoginResponse>(
                "auth/login",
                new LoginRequest(email, password, licenseKey));

            if (response is null || !response.Success)
                return new AuthResult(false, null, response?.Message ?? "Credenciais inválidas.");

            _tokenStore.AccessToken  = response.AccessToken;
            _tokenStore.RefreshToken = response.RefreshToken;
            CurrentUser = response.User;

            // Cache user for offline use
            await _db.SaveUserAsync(CurrentUser);

            return new AuthResult(true, response.AccessToken, null, CurrentUser);
        }
        catch (HttpRequestException)
        {
            // Offline fallback — try cached credentials
            var cached = await _db.GetLastUserAsync();
            if (cached is not null)
            {
                CurrentUser = cached;
                return new AuthResult(true, _tokenStore.AccessToken, "Modo offline — dados em cache.", cached);
            }

            return new AuthResult(false, null, "Sem conexão e nenhum usuário em cache.");
        }
        catch (Exception ex)
        {
            return new AuthResult(false, null, ex.Message);
        }
    }

    public Task LogoutAsync()
    {
        CurrentUser              = null;
        _tokenStore.AccessToken  = null;
        _tokenStore.RefreshToken = null;
        return Task.CompletedTask;
    }

    public async Task<bool> RefreshTokenAsync()
    {
        if (string.IsNullOrEmpty(_tokenStore.RefreshToken)) return false;
        try
        {
            var response = await _api.PostAsync<RefreshRequest, LoginResponse>(
                "auth/refresh",
                new RefreshRequest(_tokenStore.RefreshToken));

            if (response is null || !response.Success) return false;

            _tokenStore.AccessToken  = response.AccessToken;
            _tokenStore.RefreshToken = response.RefreshToken;
            return true;
        }
        catch { return false; }
    }
}

// ── DTOs ────────────────────────────────────────────────────────────────────
internal sealed record LoginRequest(string Email, string Password, string LicenseKey);
internal sealed record RefreshRequest(string RefreshToken);
internal sealed record LoginResponse(bool Success, string? Message,
    string AccessToken, string RefreshToken, User User);
