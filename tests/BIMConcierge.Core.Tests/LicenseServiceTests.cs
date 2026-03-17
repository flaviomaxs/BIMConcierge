using BIMConcierge.Core.Models;
using BIMConcierge.Infrastructure.Api;
using BIMConcierge.Infrastructure.Licensing;
using FluentAssertions;
using System.Net.Http;
using Xunit;
using License = BIMConcierge.Core.Models.License;

namespace BIMConcierge.Core.Tests;

public class LicenseServiceTests
{
    private readonly FakeBimApiClient _fakeApi = new();

    private LicenseService CreateSut() => new(_fakeApi);

    [Fact]
    public async Task ValidateAsync_ReturnsLicenseFromApi()
    {
        var license = new License
        {
            Key = "LIC-001", CompanyId = "c1", MaxSeats = 10, UsedSeats = 3,
            Type = LicenseType.Professional, ExpiresAt = DateTime.UtcNow.AddDays(30)
        };
        _fakeApi.EndpointResponses["licenses/validate/LIC-001"] = license;

        var sut = CreateSut();
        var result = await sut.ValidateAsync("LIC-001");

        result.Should().NotBeNull();
        result!.Key.Should().Be("LIC-001");
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_ApiThrows_ReturnsNull()
    {
        _fakeApi.ExceptionToThrow = new HttpRequestException("down");

        var sut = CreateSut();
        var result = await sut.ValidateAsync("LIC-001");

        result.Should().BeNull();
    }

    [Fact]
    public async Task ActivateAsync_Success_ReturnsTrue()
    {
        _fakeApi.ResponseToReturn = new ActivateResponse(true, null);

        var sut = CreateSut();
        var result = await sut.ActivateAsync("LIC-001", "u1");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task ActivateAsync_Failure_ReturnsFalse()
    {
        _fakeApi.ExceptionToThrow = new HttpRequestException("down");

        var sut = CreateSut();
        var result = await sut.ActivateAsync("LIC-001", "u1");

        result.Should().BeFalse();
    }
}
