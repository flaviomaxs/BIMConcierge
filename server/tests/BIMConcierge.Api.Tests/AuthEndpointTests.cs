using System.Net;
using System.Net.Http.Json;
using BIMConcierge.Api.Data;
using BIMConcierge.Api.Dtos;
using BIMConcierge.Api.Entities;
using BIMConcierge.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BIMConcierge.Api.Tests;

public class CustomApiFactory : WebApplicationFactory<Program>
{
    // Use a fixed name so all DbContexts resolve to the same InMemory store
    public static readonly string DbName = "TestDb_Auth";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            // Remove the real PostgreSQL registration
            var descriptors = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>)
                         || d.ServiceType == typeof(DbContextOptions))
                .ToList();
            foreach (var d in descriptors) services.Remove(d);

            // Add InMemory database with a fixed name
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(DbName));
        });
    }
}

/// <summary>
/// Tests use the seed data from AppDbContext.OnModelCreating (HasData):
/// - User: admin@bimconcierge.com / Admin123! (company-demo-001)
/// - License: BIM-DEMO-0001-0001 (Enterprise, 999 seats, expires 2027)
/// </summary>
public class AuthEndpointTests : IClassFixture<CustomApiFactory>
{
    private readonly HttpClient _client;

    // Seed data constants (must match AppDbContext.SeedData)
    private const string SeedEmail = "admin@bimconcierge.com";
    private const string SeedPassword = "Admin123!";
    private const string SeedLicenseKey = "BIM-DEMO-0001-0001";

    public AuthEndpointTests(CustomApiFactory factory)
    {
        _client = factory.CreateClient();

        // Ensure extra test licenses exist (expired + full)
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (!db.Licenses.Any(l => l.Key == "BIM-EXPIRED-0001"))
        {
            db.Licenses.Add(new LicenseEntity
            {
                Id = "expired-license",
                Key = "BIM-EXPIRED-0001",
                CompanyId = "company-demo-001",
                MaxSeats = 10,
                UsedSeats = 0,
                Type = "Trial",
                ExpiresAt = DateTime.UtcNow.AddDays(-1)
            });

            db.Licenses.Add(new LicenseEntity
            {
                Id = "full-license",
                Key = "BIM-FULL-0001",
                CompanyId = "company-demo-001",
                MaxSeats = 1,
                UsedSeats = 1,
                Type = "Professional",
                ExpiresAt = DateTime.UtcNow.AddYears(1)
            });

            db.SaveChanges();
        }
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsTokensAndUser()
    {
        var response = await _client.PostAsJsonAsync("/v1/auth/login",
            new LoginRequest(SeedEmail, SeedPassword, SeedLicenseKey));

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(result);
        Assert.True(result.Success, $"Expected success but got: {result.Message}");
        Assert.False(string.IsNullOrEmpty(result.AccessToken));
        Assert.False(string.IsNullOrEmpty(result.RefreshToken));
        Assert.Equal(SeedEmail, result.User.Email);
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsFailure()
    {
        var response = await _client.PostAsJsonAsync("/v1/auth/login",
            new LoginRequest(SeedEmail, "WrongPass!", SeedLicenseKey));

        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Contains("inválidas", result.Message);
    }

    [Fact]
    public async Task Login_ExpiredLicense_ReturnsFailure()
    {
        var response = await _client.PostAsJsonAsync("/v1/auth/login",
            new LoginRequest(SeedEmail, SeedPassword, "BIM-EXPIRED-0001"));

        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Contains("expirada", result.Message);
    }

    [Fact]
    public async Task Login_SeatsExhausted_ReturnsFailure()
    {
        var response = await _client.PostAsJsonAsync("/v1/auth/login",
            new LoginRequest(SeedEmail, SeedPassword, "BIM-FULL-0001"));

        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Contains("seats", result.Message);
    }

    [Fact]
    public async Task Login_InvalidLicenseKey_ReturnsFailure()
    {
        var response = await _client.PostAsJsonAsync("/v1/auth/login",
            new LoginRequest(SeedEmail, SeedPassword, "BIM-INVALID-0000"));

        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Contains("inválida", result.Message);
    }

    [Fact]
    public async Task Refresh_ValidToken_ReturnsNewTokens()
    {
        // First login to get a refresh token
        var loginResponse = await _client.PostAsJsonAsync("/v1/auth/login",
            new LoginRequest(SeedEmail, SeedPassword, SeedLicenseKey));
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(loginResult);
        Assert.True(loginResult.Success, $"Login failed: {loginResult.Message}");

        // Use refresh token
        var response = await _client.PostAsJsonAsync("/v1/auth/refresh",
            new RefreshRequest(loginResult.RefreshToken));

        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(result);
        Assert.True(result.Success, $"Refresh failed: {result.Message}");
        Assert.False(string.IsNullOrEmpty(result.AccessToken));
        Assert.NotEqual(loginResult.RefreshToken, result.RefreshToken);
    }

    [Fact]
    public async Task Refresh_InvalidToken_ReturnsFailure()
    {
        var response = await _client.PostAsJsonAsync("/v1/auth/refresh",
            new RefreshRequest("invalid-refresh-token"));

        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(result);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task LicenseValidate_WithoutAuth_Returns401()
    {
        var response = await _client.GetAsync("/v1/licenses/validate/BIM-DEMO-0001-0001");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
