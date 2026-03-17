using System.Net.Http;
using BIMConcierge.Core.Interfaces;
using BIMConcierge.Core.Models;
using BIMConcierge.Infrastructure.Api;
using BIMConcierge.Infrastructure.Revit;
using FluentAssertions;
using Moq;
using Xunit;

namespace BIMConcierge.Core.Tests;

public class StandardsServiceTests
{
    private readonly FakeBimApiClient _fakeApi = new();
    private readonly Mock<ILocalDatabase> _dbMock = new();
    private readonly Mock<IRevitEventDispatcher> _dispatcherMock = new();

    private StandardsService CreateSut() => new(_fakeApi, _dbMock.Object, _dispatcherMock.Object);

    // ── GetStandardsAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetStandardsAsync_ApiReturnsData_CachesAndReturnsList()
    {
        var standards = new List<CompanyStandard>
        {
            new() { Id = "s1", Name = "Naming Rule", Category = "Naming" },
            new() { Id = "s2", Name = "LOD Rule", Category = "LOD" }
        };
        _fakeApi.ResponseToReturn = standards;

        StandardsService sut = CreateSut();
        List<CompanyStandard> result = await sut.GetStandardsAsync("company1");

        result.Should().HaveCount(2);
        _dbMock.Verify(d => d.SaveStandardsAsync(It.IsAny<List<CompanyStandard>>()), Times.Once);
    }

    [Fact]
    public async Task GetStandardsAsync_ApiReturnsNull_ReturnsEmptyList()
    {
        _fakeApi.ResponseToReturn = null;

        StandardsService sut = CreateSut();
        List<CompanyStandard> result = await sut.GetStandardsAsync("company1");

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetStandardsAsync_ApiThrows_FallsBackToLocalCache()
    {
        var cached = new List<CompanyStandard>
        {
            new() { Id = "s1", Name = "Cached Standard" }
        };
        _fakeApi.ExceptionToThrow = new HttpRequestException("timeout");
        _dbMock.Setup(d => d.GetStandardsAsync("company1")).ReturnsAsync(cached);

        StandardsService sut = CreateSut();
        List<CompanyStandard> result = await sut.GetStandardsAsync("company1");

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Cached Standard");
    }

    // ── SaveStandardAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task SaveStandardAsync_CallsApiPut()
    {
        var standard = new CompanyStandard { Id = "s1", Name = "Test" };

        StandardsService sut = CreateSut();

        // Should not throw
        await sut.SaveStandardAsync(standard);
    }

    // ── DeleteStandardAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task DeleteStandardAsync_CallsApiDelete()
    {
        StandardsService sut = CreateSut();

        // Should not throw
        await sut.DeleteStandardAsync("s1");
    }

    // ── ValidateModelAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task ValidateModelAsync_NonConcreteDispatcher_ReturnsEmptyList()
    {
        // Mock is NOT a RevitEventDispatcher concrete type, so fallback applies
        StandardsService sut = CreateSut();
        List<CorrectionEvent> result = await sut.ValidateModelAsync();

        result.Should().BeEmpty();
    }

    // ── AutoFixAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task AutoFixAsync_NonConcreteDispatcher_ReturnsFalse()
    {
        StandardsService sut = CreateSut();
        bool result = await sut.AutoFixAsync("c1");

        result.Should().BeFalse();
    }
}
