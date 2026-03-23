using BIMConcierge.Core.Interfaces;
using BIMConcierge.Core.Models;
using BIMConcierge.UI.ViewModels;
using FluentAssertions;
using Moq;
using Xunit;

namespace BIMConcierge.Core.Tests;

public class AchievementsViewModelTests
{
    private readonly Mock<IProgressService> _progressMock = new();
    private readonly Mock<IAuthService> _authMock = new();
    private readonly Mock<INavigationService> _navMock = new();

    private static readonly User TestUser = new()
    {
        Id = "u1", Name = "Carlos", Level = 15, XpPoints = 1500
    };

    private AchievementsViewModel CreateSut(User? user = null, bool useNull = false)
    {
        _authMock.SetupGet(a => a.CurrentUser).Returns(useNull ? null : (user ?? TestUser));
        return new AchievementsViewModel(_progressMock.Object, _authMock.Object, _navMock.Object);
    }

    [Fact]
    public void Constructor_PopulatesUserInfo()
    {
        AchievementsViewModel sut = CreateSut();

        sut.UserName.Should().Be("Carlos");
        sut.UserLevel.Should().Be(15);
        sut.CurrentXp.Should().Be(1500);
        sut.LevelTitle.Should().Be("Level 15");
    }

    [Fact]
    public void Constructor_NullUser_LeavesDefaults()
    {
        AchievementsViewModel sut = CreateSut(useNull: true);

        sut.UserName.Should().BeEmpty();
        sut.UserLevel.Should().Be(0);
    }

    [Fact]
    public void Constructor_CalculatesLevelProgress()
    {
        AchievementsViewModel sut = CreateSut();

        // 1500 % 1000 = 500, 500 / 1000 * 100 = 50%
        sut.LevelProgressPercent.Should().Be(50);
        sut.XpForNextLevel.Should().Be(16000); // (15+1) * 1000
    }

    [Fact]
    public async Task LoadCommand_PopulatesAllCollections()
    {
        var achievements = new List<Achievement>
        {
            new() { Id = "a1", Title = "First Steps", IsUnlocked = true, XpReward = 50 },
            new() { Id = "a2", Title = "Standard Master", IsUnlocked = false, XpReward = 100 }
        };
        _progressMock.Setup(p => p.GetAchievementsAsync("u1")).ReturnsAsync(achievements);
        _progressMock.Setup(p => p.GetLeaderboardAsync(It.IsAny<string>())).ReturnsAsync(new List<LeaderboardEntry>
        {
            new() { Rank = 1, Name = "Ana", XpPoints = 2000 },
            new() { Rank = 2, Name = "Bruno", XpPoints = 1800 },
            new() { Rank = 3, Name = "Diana", XpPoints = 1600 }
        });

        AchievementsViewModel sut = CreateSut();
        await sut.LoadCommand.ExecuteAsync(null);

        sut.AllAchievements.Should().HaveCount(2);
        sut.FilteredAchievements.Should().HaveCount(2); // Default filter = "All"
        sut.ActiveQuests.Should().HaveCount(2);
        sut.Leaderboard.Should().HaveCount(4); // 3 from API + 1 current user
        sut.Rewards.Should().HaveCount(4);
        sut.IsBusy.Should().BeFalse();
    }

    [Fact]
    public async Task SetFilterCommand_FiltersByUnlocked()
    {
        var achievements = new List<Achievement>
        {
            new() { Id = "a1", Title = "First Steps", IsUnlocked = true },
            new() { Id = "a2", Title = "Expert", IsUnlocked = false },
            new() { Id = "a3", Title = "Master", IsUnlocked = true }
        };
        _progressMock.Setup(p => p.GetAchievementsAsync("u1")).ReturnsAsync(achievements);

        AchievementsViewModel sut = CreateSut();
        await sut.LoadCommand.ExecuteAsync(null);

        sut.SetFilterCommand.Execute("Unlocked");

        sut.FilterMode.Should().Be("Unlocked");
        sut.FilteredAchievements.Should().HaveCount(2);
        sut.FilteredAchievements.Should().OnlyContain(a => a.IsUnlocked);
    }

    [Fact]
    public async Task SetFilterCommand_FiltersByLocked()
    {
        var achievements = new List<Achievement>
        {
            new() { Id = "a1", Title = "First", IsUnlocked = true },
            new() { Id = "a2", Title = "Expert", IsUnlocked = false }
        };
        _progressMock.Setup(p => p.GetAchievementsAsync("u1")).ReturnsAsync(achievements);

        AchievementsViewModel sut = CreateSut();
        await sut.LoadCommand.ExecuteAsync(null);

        sut.SetFilterCommand.Execute("Locked");

        sut.FilteredAchievements.Should().HaveCount(1);
        sut.FilteredAchievements.Should().OnlyContain(a => !a.IsUnlocked);
    }

    [Fact]
    public async Task LoadCommand_RewardsUnlockBasedOnLevel()
    {
        _progressMock.Setup(p => p.GetAchievementsAsync("u1")).ReturnsAsync(new List<Achievement>());

        AchievementsViewModel sut = CreateSut(); // Level = 15
        await sut.LoadCommand.ExecuteAsync(null);

        // Level 15 -> "Tema Midnight" (10) is unlocked, "Certificação" (20) is locked
        sut.Rewards.Should().Contain(r => r.Title == "Tema Midnight" && r.IsUnlocked);
        sut.Rewards.Should().Contain(r => r.Title == "Certificação Oficial" && !r.IsUnlocked);
    }

    [Fact]
    public async Task LoadCommand_LeaderboardIncludesCurrentUser()
    {
        _progressMock.Setup(p => p.GetAchievementsAsync("u1")).ReturnsAsync(new List<Achievement>());

        AchievementsViewModel sut = CreateSut();
        await sut.LoadCommand.ExecuteAsync(null);

        sut.Leaderboard.Should().Contain(e => e.IsCurrentUser && e.Name == "Carlos" && e.XpPoints == 1500);
    }

    [Fact]
    public void OpenWindowCommand_DelegatesToNavigationService()
    {
        AchievementsViewModel sut = CreateSut();
        sut.OpenWindowCommand.Execute("Dashboard");

        _navMock.Verify(n => n.NavigateTo("Dashboard"), Times.Once);
    }

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        AchievementsViewModel sut = CreateSut();
        Action act = () => sut.Dispose();
        act.Should().NotThrow();
    }
}
