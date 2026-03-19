using BIMConcierge.Core.Interfaces;
using BIMConcierge.Core.Models;
using BIMConcierge.Infrastructure.Api;
using BIMConcierge.Infrastructure.Revit;
using FluentAssertions;
using Moq;
using Xunit;

namespace BIMConcierge.Integration.Tests;

/// <summary>
/// Integration tests that use the concrete RevitEventDispatcher
/// (unlike unit tests that mock IRevitEventDispatcher).
/// </summary>
public class StandardsServiceIntegrationTests : IDisposable
{
    private readonly Mock<ILocalDatabase> _dbMock = new();
    private readonly FakeBimApiClient _fakeApi = new();
    private readonly RevitEventDispatcher _dispatcher;
    private readonly StandardsService _sut;

    public StandardsServiceIntegrationTests()
    {
        _dispatcher = new RevitEventDispatcher(_dbMock.Object);
        _sut = new StandardsService(_fakeApi, _dbMock.Object, _dispatcher);
    }

    public void Dispose()
    {
        _dispatcher.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task ValidateModelAsync_WithConcreteDispatcher_ReturnsCorrections()
    {
        // Load standards into dispatcher
        var standards = new List<CompanyStandard>
        {
            new()
            {
                Id = "s1", CompanyId = "c1", Category = "Walls",
                Name = "Wall Naming", Rule = "^PRJ-.*$",
                IsActive = true, AlertLevel = Severity.Error
            }
        };
        _dbMock.Setup(d => d.GetStandardsAsync("c1")).ReturnsAsync(standards);
        await _dispatcher.LoadStandardsAsync("c1");

        // Simulate elements being validated (as RevitEventBridge would do)
        _dispatcher.ValidateElements([("e1", "BadWall", "Walls"), ("e2", "PRJ-GoodWall", "Walls")]);

        // Now StandardsService.ValidateModelAsync should return the active corrections
        var result = await _sut.ValidateModelAsync();

        result.Should().HaveCount(1);
        result[0].ElementId.Should().Be("e1");
        result[0].Severity.Should().Be(Severity.Error);
    }

    [Fact]
    public async Task AutoFixAsync_WithConcreteDispatcher_DelegatesToHandler()
    {
        bool handlerCalled = false;
        _dispatcher.RegisterAutoFixHandler((elementId, rule) =>
        {
            handlerCalled = true;
            return Task.FromResult(true);
        });

        var standards = new List<CompanyStandard>
        {
            new()
            {
                Id = "s1", CompanyId = "c1", Category = "Walls",
                Name = "Wall Naming", Rule = "^PRJ-.*$",
                IsActive = true, AutoFix = true
            }
        };
        _dbMock.Setup(d => d.GetStandardsAsync("c1")).ReturnsAsync(standards);
        await _dispatcher.LoadStandardsAsync("c1");

        var corrections = _dispatcher.ValidateElements([("e1", "BadWall", "Walls")]);
        corrections.Should().HaveCount(1);

        var result = await _sut.AutoFixAsync(corrections[0].Id);

        result.Should().BeTrue();
        handlerCalled.Should().BeTrue();
    }

    [Fact]
    public async Task GetStandards_ThenValidate_EndToEnd()
    {
        var standards = new List<CompanyStandard>
        {
            new()
            {
                Id = "s1", CompanyId = "c1", Category = "Families",
                Name = "Family Naming", Rule = "^FAM-.*$",
                IsActive = true, AlertLevel = Severity.Warning
            },
            new()
            {
                Id = "s2", CompanyId = "c1", Category = "Walls",
                Name = "Wall Naming", Rule = "^W-.*$",
                IsActive = true, AlertLevel = Severity.Error
            }
        };

        // API returns standards
        _fakeApi.ResponseToReturn = standards;

        // Fetch from service (simulates what the app does on login)
        var fetched = await _sut.GetStandardsAsync("c1");
        fetched.Should().HaveCount(2);

        // Load into dispatcher (simulates what the app does after fetching)
        _dbMock.Setup(d => d.GetStandardsAsync("c1")).ReturnsAsync(standards);
        await _dispatcher.LoadStandardsAsync("c1");

        // Validate elements
        _dispatcher.ValidateElements([
            ("e1", "BadFamily", "Families"),
            ("e2", "FAM-GoodFamily", "Families"),
            ("e3", "BadWall", "Walls")
        ]);

        var result = await _sut.ValidateModelAsync();

        result.Should().HaveCount(2);
        result.Should().Contain(c => c.ElementId == "e1" && c.Severity == Severity.Warning);
        result.Should().Contain(c => c.ElementId == "e3" && c.Severity == Severity.Error);
    }
}

/// <summary>
/// Reusable fake API client for integration tests (same pattern as Core.Tests).
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
