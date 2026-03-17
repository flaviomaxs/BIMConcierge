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
    private readonly string _dbName = "TestDb_" + Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            // Remove the real PostgreSQL registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null) services.Remove(descriptor);

            // Add InMemory database with a fixed name per factory instance
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));

            // Seed test data after the service provider is built
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
            SeedTestData(db);
        });
    }

    private static void SeedTestData(AppDbContext db)
    {
        if (db.Companies.Any()) return;

        var company = new Company { Id = "test-company", Name = "Test Co" };
        db.Companies.Add(company);

        db.Users.Add(new UserEntity
        {
            Id = "test-user",
            Email = "test@bimconcierge.com",
            PasswordHash = PasswordHasher.Hash("Test123!"),
            Name = "Test User",
            Role = "Admin",
            CompanyId = "test-company"
        });

        db.Licenses.Add(new LicenseEntity
        {
            Id = "test-license",
            Key = "BIM-TEST-0001-0001",
            CompanyId = "test-company",
            MaxSeats = 10,
            UsedSeats = 0,
            Type = "Enterprise",
            ExpiresAt = DateTime.UtcNow.AddYears(1)
        });

        db.Licenses.Add(new LicenseEntity
        {
            Id = "expired-license",
            Key = "BIM-EXPIRED-0001",
            CompanyId = "test-company",
            MaxSeats = 10,
            UsedSeats = 0,
            Type = "Trial",
            ExpiresAt = DateTime.UtcNow.AddDays(-1)
        });

        db.Licenses.Add(new LicenseEntity
        {
            Id = "full-license",
            Key = "BIM-FULL-0001",
            CompanyId = "test-company",
            MaxSeats = 1,
            UsedSeats = 1,
            Type = "Professional",
            ExpiresAt = DateTime.UtcNow.AddYears(1)
        });

        db.SaveChanges();
    }
}

public class AuthEndpointTests : IClassFixture<CustomApiFactory>
{
    private readonly HttpClient _client;

    public AuthEndpointTests(CustomApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsTokensAndUser()
    {
        var response = await _client.PostAsJsonAsync("/v1/auth/login",
            new LoginRequest("test@bimconcierge.com", "Test123!", "BIM-TEST-0001-0001"));

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.False(string.IsNullOrEmpty(result.AccessToken));
        Assert.False(string.IsNullOrEmpty(result.RefreshToken));
        Assert.Equal("test@bimconcierge.com", result.User.Email);
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsFailure()
    {
        var response = await _client.PostAsJsonAsync("/v1/auth/login",
            new LoginRequest("test@bimconcierge.com", "WrongPass!", "BIM-TEST-0001-0001"));

        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Contains("inválidas", result.Message);
    }

    [Fact]
    public async Task Login_ExpiredLicense_ReturnsFailure()
    {
        var response = await _client.PostAsJsonAsync("/v1/auth/login",
            new LoginRequest("test@bimconcierge.com", "Test123!", "BIM-EXPIRED-0001"));

        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Contains("expirada", result.Message);
    }

    [Fact]
    public async Task Login_SeatsExhausted_ReturnsFailure()
    {
        var response = await _client.PostAsJsonAsync("/v1/auth/login",
            new LoginRequest("test@bimconcierge.com", "Test123!", "BIM-FULL-0001"));

        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Contains("seats", result.Message);
    }

    [Fact]
    public async Task Login_InvalidLicenseKey_ReturnsFailure()
    {
        var response = await _client.PostAsJsonAsync("/v1/auth/login",
            new LoginRequest("test@bimconcierge.com", "Test123!", "BIM-INVALID-0000"));

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
            new LoginRequest("test@bimconcierge.com", "Test123!", "BIM-TEST-0001-0001"));
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
        var response = await _client.GetAsync("/v1/licenses/validate/BIM-TEST-0001-0001");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
