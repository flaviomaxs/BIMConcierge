using BIMConcierge.Core.Interfaces;
using BIMConcierge.Core.Models;
using BIMConcierge.Infrastructure.Auth;
using BIMConcierge.Infrastructure.Api;
using BIMConcierge.Infrastructure.Licensing;
using FluentAssertions;
using Moq;
using Xunit;
using System.Net.Http;
using System.Threading.Tasks;
using License = BIMConcierge.Core.Models.License;

namespace BIMConcierge.Core.Tests;

/// <summary>
/// Helper that implements IBimApiClient to control behavior in tests
/// without fighting Moq's generic type constraints.
/// </summary>
internal sealed class FakeBimApiClient : IBimApiClient
{
    public Exception? ExceptionToThrow { get; set; }
    public object? ResponseToReturn { get; set; }
    public Dictionary<string, object?> EndpointResponses { get; } = new();

    public Task<TResponse?> GetAsync<TResponse>(string endpoint) =>
        Execute<TResponse>(endpoint);

    public Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest body) =>
        Execute<TResponse>(endpoint);

    public Task<TResponse?> PutAsync<TRequest, TResponse>(string endpoint, TRequest body) =>
        Execute<TResponse>(endpoint);

    public Task DeleteAsync(string endpoint) =>
        ExceptionToThrow is not null ? throw ExceptionToThrow : Task.CompletedTask;

    private Task<TResponse?> Execute<TResponse>(string endpoint)
    {
        if (ExceptionToThrow is not null) throw ExceptionToThrow;
        if (EndpointResponses.TryGetValue(endpoint, out var specific) && specific is TResponse typed)
            return Task.FromResult<TResponse?>(typed);
        return Task.FromResult(ResponseToReturn is TResponse r ? r : default(TResponse?));
    }
}

public class AuthServiceTests
{
    private readonly FakeBimApiClient _fakeApi = new();
    private readonly Mock<ITokenStore> _tokenMock = new();
    private readonly Mock<ILocalDatabase> _dbMock = new();
    private readonly Mock<ILicenseService> _licenseMock = new();

    public AuthServiceTests()
    {
        // Reset static state that persists between tests
        var sut = CreateSut();
        sut.LogoutAsync().GetAwaiter().GetResult();
    }

    private AuthService CreateSut() =>
        new(_fakeApi, _tokenMock.Object, _dbMock.Object, _licenseMock.Object);

    private static License ValidLicense() => new()
    {
        Key = "LIC-001", CompanyId = "c1", MaxSeats = 10, UsedSeats = 3,
        Type = LicenseType.Professional, ExpiresAt = DateTime.UtcNow.AddDays(30)
    };

    private static License ExpiredLicense() => new()
    {
        Key = "LIC-EXP", CompanyId = "c1", MaxSeats = 10, UsedSeats = 3,
        Type = LicenseType.Professional, ExpiresAt = DateTime.UtcNow.AddDays(-1)
    };

    private static License FullSeatsLicense() => new()
    {
        Key = "LIC-FULL", CompanyId = "c1", MaxSeats = 5, UsedSeats = 5,
        Type = LicenseType.Professional, ExpiresAt = DateTime.UtcNow.AddDays(30)
    };

    // ── Initial state ──────────────────────────────────────────────────────

    [Fact]
    public void IsAuthenticated_BeforeLogin_ReturnsFalse()
    {
        var sut = CreateSut();
        sut.IsAuthenticated.Should().BeFalse();
        sut.CurrentUser.Should().BeNull();
        sut.CurrentLicense.Should().BeNull();
    }

    // ── Login — API returns null ───────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_ApiReturnsNull_ReturnsFailure()
    {
        _fakeApi.ResponseToReturn = null;

        var sut = CreateSut();
        var result = await sut.LoginAsync("a@b.com", "pass", "key");

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    // ── Login — success with valid license ───────────────────────────────

    [Fact]
    public async Task LoginAsync_Success_WithValidLicense_ReturnsSuccessWithLicense()
    {
        var user = new User { Id = "u1", Name = "Ana", Email = "ana@empresa.com", CompanyId = "c1" };
        _fakeApi.ResponseToReturn = new LoginResponse(true, null, "tok", "ref", user);

        var license = ValidLicense();
        _licenseMock.Setup(l => l.ValidateAsync("key")).ReturnsAsync(license);

        var sut = CreateSut();
        var result = await sut.LoginAsync("ana@empresa.com", "pass", "key");

        result.Success.Should().BeTrue();
        result.User.Should().Be(user);
        result.License.Should().Be(license);
        sut.CurrentLicense.Should().Be(license);
    }

    // ── Login — expired license ──────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_ExpiredLicense_ReturnsFailure()
    {
        var user = new User { Id = "u1", Name = "Ana", Email = "ana@empresa.com", CompanyId = "c1" };
        _fakeApi.ResponseToReturn = new LoginResponse(true, null, "tok", "ref", user);

        _licenseMock.Setup(l => l.ValidateAsync("key")).ReturnsAsync(ExpiredLicense());

        var sut = CreateSut();
        var result = await sut.LoginAsync("ana@empresa.com", "pass", "key");

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("expirada");
    }

    // ── Login — seats exhausted ──────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_SeatsExhausted_ReturnsFailure()
    {
        var user = new User { Id = "u1", Name = "Ana", Email = "ana@empresa.com", CompanyId = "c1" };
        _fakeApi.ResponseToReturn = new LoginResponse(true, null, "tok", "ref", user);

        _licenseMock.Setup(l => l.ValidateAsync("key")).ReturnsAsync(FullSeatsLicense());

        var sut = CreateSut();
        var result = await sut.LoginAsync("ana@empresa.com", "pass", "key");

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("seats");
    }

    // ── Login — invalid license key ──────────────────────────────────────

    [Fact]
    public async Task LoginAsync_InvalidLicenseKey_ReturnsFailure()
    {
        var user = new User { Id = "u1", Name = "Ana", Email = "ana@empresa.com", CompanyId = "c1" };
        _fakeApi.ResponseToReturn = new LoginResponse(true, null, "tok", "ref", user);

        _licenseMock.Setup(l => l.ValidateAsync("bad-key")).ReturnsAsync((License?)null);

        var sut = CreateSut();
        var result = await sut.LoginAsync("ana@empresa.com", "pass", "bad-key");

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("licença");
    }

    // ── Login — network failure with cached user + license (offline) ─────

    [Fact]
    public async Task LoginAsync_NetworkFailure_WithCachedUserAndLicense_ReturnsOfflineSuccess()
    {
        var cachedUser = new User { Id = "u1", Name = "Carlos", Email = "carlos@empresa.com", CompanyId = "c1" };
        var cachedLicense = ValidLicense();

        _fakeApi.ExceptionToThrow = new HttpRequestException("No network");
        _dbMock.Setup(d => d.GetLastUserAsync()).ReturnsAsync(cachedUser);
        _dbMock.Setup(d => d.GetCachedLicenseAsync("c1")).ReturnsAsync(cachedLicense);

        var sut = CreateSut();
        var result = await sut.LoginAsync("carlos@empresa.com", "pass", "key");

        result.Success.Should().BeTrue();
        result.User.Should().Be(cachedUser);
        result.License.Should().Be(cachedLicense);
        result.ErrorMessage.Should().Contain("offline");
    }

    // ── Login — network failure with cached expired license ──────────────

    [Fact]
    public async Task LoginAsync_NetworkFailure_WithExpiredCachedLicense_ReturnsFailure()
    {
        var cachedUser = new User { Id = "u1", Name = "Carlos", Email = "carlos@empresa.com", CompanyId = "c1" };

        _fakeApi.ExceptionToThrow = new HttpRequestException("No network");
        _dbMock.Setup(d => d.GetLastUserAsync()).ReturnsAsync(cachedUser);
        _dbMock.Setup(d => d.GetCachedLicenseAsync("c1")).ReturnsAsync(ExpiredLicense());

        var sut = CreateSut();
        var result = await sut.LoginAsync("carlos@empresa.com", "pass", "key");

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("expirada");
    }

    // ── Login — network failure without cached user ──────────────────────

    [Fact]
    public async Task LoginAsync_NetworkFailure_NoCachedUser_ReturnsFailure()
    {
        _fakeApi.ExceptionToThrow = new HttpRequestException("No network");
        _dbMock.Setup(d => d.GetLastUserAsync()).ReturnsAsync((User?)null);

        var sut = CreateSut();
        var result = await sut.LoginAsync("a@b.com", "pass", "key");

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Sem conexão");
    }

    // ── Login — unexpected exception ─────────────────────────────────────

    [Fact]
    public async Task LoginAsync_UnexpectedException_ReturnsFailureWithMessage()
    {
        _fakeApi.ExceptionToThrow = new InvalidOperationException("boom");

        var sut = CreateSut();
        var result = await sut.LoginAsync("a@b.com", "pass", "key");

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("boom");
    }

    // ── Logout ───────────────────────────────────────────────────────────

    [Fact]
    public async Task LogoutAsync_ClearsTokensUserAndLicense()
    {
        var sut = CreateSut();
        // Reset mock call counts after constructor cleanup
        _tokenMock.Invocations.Clear();

        await sut.LogoutAsync();

        sut.CurrentUser.Should().BeNull();
        sut.CurrentLicense.Should().BeNull();
        sut.IsAuthenticated.Should().BeFalse();
        _tokenMock.VerifySet(t => t.AccessToken = null, Times.Once);
        _tokenMock.VerifySet(t => t.RefreshToken = null, Times.Once);
    }

    // ── EnsureValidSessionAsync ──────────────────────────────────────────

    [Fact]
    public async Task EnsureValidSessionAsync_NoUser_ReturnsFalse()
    {
        var sut = CreateSut();
        var result = await sut.EnsureValidSessionAsync();
        result.Should().BeFalse();
    }

    // ── Refresh token ────────────────────────────────────────────────────

    [Fact]
    public async Task RefreshTokenAsync_NoRefreshToken_ReturnsFalse()
    {
        _tokenMock.SetupGet(t => t.RefreshToken).Returns((string?)null);

        var sut = CreateSut();
        var result = await sut.RefreshTokenAsync();

        result.Should().BeFalse();
    }

    [Fact]
    public async Task RefreshTokenAsync_EmptyRefreshToken_ReturnsFalse()
    {
        _tokenMock.SetupGet(t => t.RefreshToken).Returns(string.Empty);

        var sut = CreateSut();
        var result = await sut.RefreshTokenAsync();

        result.Should().BeFalse();
    }

    [Fact]
    public async Task RefreshTokenAsync_ApiFailure_ReturnsFalse()
    {
        _tokenMock.SetupGet(t => t.RefreshToken).Returns("some-refresh-token");
        _fakeApi.ExceptionToThrow = new HttpRequestException("timeout");

        var sut = CreateSut();
        var result = await sut.RefreshTokenAsync();

        result.Should().BeFalse();
    }
}
