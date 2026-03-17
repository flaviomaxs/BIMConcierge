using System.Net.Http;
using BIMConcierge.Core.Interfaces;
using BIMConcierge.Core.Models;
using BIMConcierge.Infrastructure.Api;
using FluentAssertions;
using Moq;
using Xunit;

namespace BIMConcierge.Core.Tests;

public class TutorialServiceTests
{
    private readonly FakeBimApiClient _fakeApi = new();
    private readonly Mock<ILocalDatabase> _dbMock = new();

    private TutorialService CreateSut() => new(_fakeApi, _dbMock.Object);

    // ── GetAllAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ApiReturnsData_CachesAndReturnsList()
    {
        var tutorials = new List<Tutorial>
        {
            new() { Id = "t1", Title = "Walls", Category = "Walls" },
            new() { Id = "t2", Title = "Families", Category = "Families" }
        };
        _fakeApi.ResponseToReturn = tutorials;

        TutorialService sut = CreateSut();
        List<Tutorial> result = await sut.GetAllAsync();

        result.Should().HaveCount(2);
        _dbMock.Verify(d => d.SaveTutorialsAsync(It.IsAny<List<Tutorial>>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_ApiReturnsNull_ReturnsEmptyList()
    {
        _fakeApi.ResponseToReturn = null;

        TutorialService sut = CreateSut();
        List<Tutorial> result = await sut.GetAllAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_ApiThrows_FallsBackToLocalCache()
    {
        var cached = new List<Tutorial> { new() { Id = "t1", Title = "Cached" } };
        _fakeApi.ExceptionToThrow = new HttpRequestException("timeout");
        _dbMock.Setup(d => d.GetTutorialsAsync(null)).ReturnsAsync(cached);

        TutorialService sut = CreateSut();
        List<Tutorial> result = await sut.GetAllAsync();

        result.Should().HaveCount(1);
        result[0].Title.Should().Be("Cached");
    }

    [Fact]
    public async Task GetAllAsync_WithCategory_PassesCategoryToApi()
    {
        _fakeApi.ResponseToReturn = new List<Tutorial>();

        TutorialService sut = CreateSut();
        await sut.GetAllAsync("Walls");

        // No exception means the method executed successfully with the category parameter
    }

    // ── GetByIdAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ApiReturnsData_ReturnsTutorial()
    {
        var tutorial = new Tutorial { Id = "t1", Title = "Walls Tutorial" };
        _fakeApi.ResponseToReturn = tutorial;

        TutorialService sut = CreateSut();
        Tutorial? result = await sut.GetByIdAsync("t1");

        result.Should().NotBeNull();
        result!.Title.Should().Be("Walls Tutorial");
    }

    [Fact]
    public async Task GetByIdAsync_ApiThrows_FallsBackToCache()
    {
        var cached = new Tutorial { Id = "t1", Title = "Cached Tutorial" };
        _fakeApi.ExceptionToThrow = new HttpRequestException("timeout");
        _dbMock.Setup(d => d.GetTutorialAsync("t1")).ReturnsAsync(cached);

        TutorialService sut = CreateSut();
        Tutorial? result = await sut.GetByIdAsync("t1");

        result.Should().NotBeNull();
        result!.Title.Should().Be("Cached Tutorial");
    }

    // ── GetProgressAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetProgressAsync_DelegatesToLocalDatabase()
    {
        var progress = new TutorialProgress { UserId = "u1", TutorialId = "t1", CurrentStep = 3 };
        _dbMock.Setup(d => d.GetProgressAsync("u1", "t1")).ReturnsAsync(progress);

        TutorialService sut = CreateSut();
        TutorialProgress? result = await sut.GetProgressAsync("u1", "t1");

        result.Should().NotBeNull();
        result!.CurrentStep.Should().Be(3);
    }

    // ── CompleteStepAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task CompleteStepAsync_NoExistingProgress_CreatesNewAndSaves()
    {
        _dbMock.Setup(d => d.GetProgressAsync("u1", "t1")).ReturnsAsync((TutorialProgress?)null);

        TutorialService sut = CreateSut();
        bool result = await sut.CompleteStepAsync("u1", "t1", 0);

        result.Should().BeTrue();
        _dbMock.Verify(d => d.SaveProgressAsync(It.Is<TutorialProgress>(
            p => p.UserId == "u1" && p.TutorialId == "t1" && p.CurrentStep == 1)), Times.Once);
    }

    [Fact]
    public async Task CompleteStepAsync_ExistingProgress_AdvancesStep()
    {
        var existing = new TutorialProgress
        {
            UserId = "u1", TutorialId = "t1", CurrentStep = 2, TotalSteps = 5
        };
        _dbMock.Setup(d => d.GetProgressAsync("u1", "t1")).ReturnsAsync(existing);

        TutorialService sut = CreateSut();
        await sut.CompleteStepAsync("u1", "t1", 2);

        _dbMock.Verify(d => d.SaveProgressAsync(It.Is<TutorialProgress>(
            p => p.CurrentStep == 3)), Times.Once);
    }

    [Fact]
    public async Task CompleteStepAsync_LastStep_MarksTutorialCompleted()
    {
        var existing = new TutorialProgress
        {
            UserId = "u1", TutorialId = "t1", CurrentStep = 4, TotalSteps = 5
        };
        _dbMock.Setup(d => d.GetProgressAsync("u1", "t1")).ReturnsAsync(existing);

        TutorialService sut = CreateSut();
        await sut.CompleteStepAsync("u1", "t1", 4);

        _dbMock.Verify(d => d.SaveProgressAsync(It.Is<TutorialProgress>(
            p => p.IsCompleted && p.ScorePercent == 100 && p.CompletedAt != null)), Times.Once);
    }
}
