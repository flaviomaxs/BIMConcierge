using System.Net.Http;
using BIMConcierge.Core.Interfaces;
using BIMConcierge.Core.Models;
using BIMConcierge.Infrastructure.Api;
using FluentAssertions;
using Moq;
using Xunit;

namespace BIMConcierge.Core.Tests;

public class ProgressServiceTests
{
    private readonly FakeBimApiClient _fakeApi = new();
    private readonly Mock<ILocalDatabase> _dbMock = new();

    private ProgressService CreateSut() => new(_fakeApi, _dbMock.Object);

    // ── GetUserProgressAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetUserProgressAsync_ApiReturnsData_ReturnsList()
    {
        var list = new List<TutorialProgress>
        {
            new() { UserId = "u1", TutorialId = "t1", CurrentStep = 2, TotalSteps = 5 },
            new() { UserId = "u1", TutorialId = "t2", CurrentStep = 3, TotalSteps = 3, IsCompleted = true }
        };
        _fakeApi.ResponseToReturn = list;

        ProgressService sut = CreateSut();
        List<TutorialProgress> result = await sut.GetUserProgressAsync("u1");

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetUserProgressAsync_ApiReturnsNull_ReturnsEmptyList()
    {
        _fakeApi.ResponseToReturn = null;

        ProgressService sut = CreateSut();
        List<TutorialProgress> result = await sut.GetUserProgressAsync("u1");

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUserProgressAsync_ApiThrows_FallsBackToLocalCache()
    {
        var cached = new List<TutorialProgress>
        {
            new() { UserId = "u1", TutorialId = "t1", CurrentStep = 1, TotalSteps = 3 }
        };
        _fakeApi.ExceptionToThrow = new HttpRequestException("timeout");
        _dbMock.Setup(d => d.GetAllProgressAsync("u1")).ReturnsAsync(cached);

        ProgressService sut = CreateSut();
        List<TutorialProgress> result = await sut.GetUserProgressAsync("u1");

        result.Should().HaveCount(1);
    }

    // ── GetAchievementsAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetAchievementsAsync_ApiReturnsData_ReturnsList()
    {
        var achievements = new List<Achievement>
        {
            new() { Id = "a1", Title = "First Steps", IsUnlocked = true },
            new() { Id = "a2", Title = "Expert", IsUnlocked = false }
        };
        _fakeApi.ResponseToReturn = achievements;

        ProgressService sut = CreateSut();
        List<Achievement> result = await sut.GetAchievementsAsync("u1");

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAchievementsAsync_ApiThrows_ReturnsEmptyList()
    {
        _fakeApi.ExceptionToThrow = new HttpRequestException("timeout");

        ProgressService sut = CreateSut();
        List<Achievement> result = await sut.GetAchievementsAsync("u1");

        result.Should().BeEmpty();
    }

    // ── UnlockAchievementAsync ──────────────────────────────────────────────

    [Fact]
    public async Task UnlockAchievementAsync_CallsApiEndpoint()
    {
        ProgressService sut = CreateSut();

        // Should not throw
        await sut.UnlockAchievementAsync("u1", "a1");
    }

    // ── AddXpAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task AddXpAsync_CallsApiEndpoint()
    {
        ProgressService sut = CreateSut();

        // Should not throw
        await sut.AddXpAsync("u1", 50);
    }
}
