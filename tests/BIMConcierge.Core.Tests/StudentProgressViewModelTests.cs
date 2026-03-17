using BIMConcierge.Core.Interfaces;
using BIMConcierge.Core.Models;
using BIMConcierge.UI.ViewModels;
using FluentAssertions;
using Moq;
using Xunit;

namespace BIMConcierge.Core.Tests;

public class StudentProgressViewModelTests
{
    private readonly Mock<IProgressService> _progressMock = new();
    private readonly Mock<ITutorialService> _tutorialMock = new();
    private readonly Mock<IAuthService> _authMock = new();
    private readonly Mock<INavigationService> _navMock = new();

    private static readonly User TestUser = new()
    {
        Id = "u1", Name = "Carlos", Role = "Collaborator",
        Level = 5, XpPoints = 1500
    };

    private StudentProgressViewModel CreateSut(User? user = null, bool useNull = false)
    {
        _authMock.SetupGet(a => a.CurrentUser).Returns(useNull ? null : (user ?? TestUser));
        return new StudentProgressViewModel(
            _progressMock.Object, _tutorialMock.Object,
            _authMock.Object, _navMock.Object);
    }

    [Fact]
    public void Constructor_PopulatesUserInfo()
    {
        StudentProgressViewModel sut = CreateSut();

        sut.UserName.Should().Be("Carlos");
        sut.UserRole.Should().Be("Collaborator");
        sut.UserLevel.Should().Be(5);
        sut.XpPoints.Should().Be(1500);
    }

    [Fact]
    public void Constructor_NullUser_LeavesDefaults()
    {
        StudentProgressViewModel sut = CreateSut(useNull: true);

        sut.UserName.Should().BeEmpty();
        sut.UserLevel.Should().Be(0);
    }

    [Fact]
    public async Task LoadCommand_PopulatesAllCollections()
    {
        var tutorials = new List<Tutorial>
        {
            new() { Id = "t1", Title = "Walls", Category = "Walls", StepCount = 5 },
            new() { Id = "t2", Title = "Families", Category = "Families", StepCount = 10 }
        };
        var progress = new List<TutorialProgress>
        {
            new() { UserId = "u1", TutorialId = "t1", CurrentStep = 3, TotalSteps = 5, IsCompleted = false },
            new() { UserId = "u1", TutorialId = "t2", CurrentStep = 10, TotalSteps = 10, IsCompleted = true, ScorePercent = 90 }
        };
        var achievements = new List<Achievement>
        {
            new() { Id = "a1", Title = "First", IsUnlocked = true, UnlockedAt = DateTime.UtcNow }
        };

        _tutorialMock.Setup(t => t.GetAllAsync(null)).ReturnsAsync(tutorials);
        _progressMock.Setup(p => p.GetUserProgressAsync("u1")).ReturnsAsync(progress);
        _progressMock.Setup(p => p.GetAchievementsAsync("u1")).ReturnsAsync(achievements);

        StudentProgressViewModel sut = CreateSut();
        await sut.LoadCommand.ExecuteAsync(null);

        sut.CompletedCount.Should().Be(1);
        sut.TotalCount.Should().Be(2);
        sut.CertificateCount.Should().Be(1); // ScorePercent >= 80
        sut.InProgressTutorials.Should().HaveCount(1);
        sut.RecentAchievements.Should().HaveCount(1);
        sut.Skills.Should().HaveCount(2); // Walls + Families
        sut.IsBusy.Should().BeFalse();
    }

    [Fact]
    public async Task LoadCommand_CalculatesSkillProficiency()
    {
        var tutorials = new List<Tutorial>
        {
            new() { Id = "t1", Category = "Walls", StepCount = 10 },
            new() { Id = "t2", Category = "Walls", StepCount = 10 }
        };
        var progress = new List<TutorialProgress>
        {
            new() { TutorialId = "t1", CurrentStep = 5, TotalSteps = 10 },
            new() { TutorialId = "t2", CurrentStep = 10, TotalSteps = 10, IsCompleted = true }
        };

        _tutorialMock.Setup(t => t.GetAllAsync(null)).ReturnsAsync(tutorials);
        _progressMock.Setup(p => p.GetUserProgressAsync("u1")).ReturnsAsync(progress);
        _progressMock.Setup(p => p.GetAchievementsAsync("u1")).ReturnsAsync(new List<Achievement>());

        StudentProgressViewModel sut = CreateSut();
        await sut.LoadCommand.ExecuteAsync(null);

        // 15 completed steps out of 20 total = 75%
        sut.Skills.Should().HaveCount(1);
        sut.Skills[0].Name.Should().Be("Walls");
        sut.Skills[0].Percent.Should().Be(75);
    }

    [Fact]
    public async Task LoadCommand_LearningHoursCalculation()
    {
        var tutorials = new List<Tutorial>();
        var progress = new List<TutorialProgress>
        {
            new() { TutorialId = "t1", TotalSteps = 4 },
            new() { TutorialId = "t2", TotalSteps = 6 }
        };

        _tutorialMock.Setup(t => t.GetAllAsync(null)).ReturnsAsync(tutorials);
        _progressMock.Setup(p => p.GetUserProgressAsync("u1")).ReturnsAsync(progress);
        _progressMock.Setup(p => p.GetAchievementsAsync("u1")).ReturnsAsync(new List<Achievement>());

        StudentProgressViewModel sut = CreateSut();
        await sut.LoadCommand.ExecuteAsync(null);

        // (4 + 6) * 0.5 = 5 hours
        sut.LearningHours.Should().Be(5);
    }

    [Fact]
    public void OpenWindowCommand_DelegatesToNavigationService()
    {
        StudentProgressViewModel sut = CreateSut();
        sut.OpenWindowCommand.Execute("Achievements");

        _navMock.Verify(n => n.NavigateTo("Achievements"), Times.Once);
    }

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        StudentProgressViewModel sut = CreateSut();
        Action act = () => sut.Dispose();
        act.Should().NotThrow();
    }
}
