using BIMConcierge.Core.Interfaces;
using BIMConcierge.Core.Models;
using BIMConcierge.UI.ViewModels;
using FluentAssertions;
using Moq;
using Xunit;

namespace BIMConcierge.Core.Tests;

public class DashboardViewModelTests
{
    private readonly Mock<IAuthService> _authMock = new();
    private readonly Mock<ITutorialService> _tutorialMock = new();
    private readonly Mock<IProgressService> _progressMock = new();
    private readonly Mock<IStandardsService> _standardsMock = new();
    private readonly Mock<INavigationService> _navMock = new();

    private static readonly User TestUser = new()
    {
        Id = "u1", Name = "Carlos", Email = "carlos@test.com",
        CompanyId = "c1", XpPoints = 500, Level = 3
    };

    private DashboardViewModel CreateSut(User? user = null, bool useNull = false)
    {
        _authMock.SetupGet(a => a.CurrentUser).Returns(useNull ? null : (user ?? TestUser));
        return new DashboardViewModel(
            _authMock.Object, _tutorialMock.Object,
            _progressMock.Object, _standardsMock.Object,
            _navMock.Object);
    }

    [Fact]
    public void Constructor_SetsCurrentUserFromAuth()
    {
        DashboardViewModel sut = CreateSut();
        sut.CurrentUser.Should().Be(TestUser);
    }

    [Fact]
    public void NavigateToCommand_SetsActiveSection()
    {
        DashboardViewModel sut = CreateSut();
        sut.NavigateToCommand.Execute("Tutorials");

        sut.ActiveSection.Should().Be("Tutorials");
    }

    [Fact]
    public void OpenWindowCommand_DelegatesToNavigationService()
    {
        DashboardViewModel sut = CreateSut();
        sut.OpenWindowCommand.Execute("CompanyStandards");

        _navMock.Verify(n => n.NavigateTo("CompanyStandards"), Times.Once);
    }

    [Fact]
    public void SelectTutorialCommand_SetsSelectedTutorialAndSection()
    {
        var tutorial = new Tutorial { Id = "t1", Title = "Test" };
        DashboardViewModel sut = CreateSut();
        sut.SelectTutorialCommand.Execute(tutorial);

        sut.SelectedTutorial.Should().Be(tutorial);
        sut.ActiveSection.Should().Be("TutorialDetail");
    }

    [Fact]
    public async Task LoadCommand_PopulatesTutorialsAndProgress()
    {
        var tutorials = new List<Tutorial>
        {
            new() { Id = "t1", Title = "Tutorial 1" },
            new() { Id = "t2", Title = "Tutorial 2" }
        };
        var progress = new List<TutorialProgress>
        {
            new() { UserId = "u1", TutorialId = "t1", IsCompleted = true }
        };
        var achievements = new List<Achievement>
        {
            new() { Id = "a1", Title = "First", IsUnlocked = true }
        };

        _tutorialMock.Setup(t => t.GetAllAsync(null)).ReturnsAsync(tutorials);
        _progressMock.Setup(p => p.GetUserProgressAsync("u1")).ReturnsAsync(progress);
        _progressMock.Setup(p => p.GetAchievementsAsync("u1")).ReturnsAsync(achievements);

        DashboardViewModel sut = CreateSut();
        await sut.LoadCommand.ExecuteAsync(null);

        sut.TutorialList.Should().HaveCount(2);
        sut.TotalTutorials.Should().Be(2);
        sut.CompletedTutorials.Should().Be(1);
        sut.ProgressList.Should().HaveCount(1);
        sut.Achievements.Should().HaveCount(1);
        sut.XpPoints.Should().Be(500);
        sut.IsBusy.Should().BeFalse();
    }

    [Fact]
    public async Task LoadCommand_NullUser_SkipsProgressAndAchievements()
    {
        var tutorials = new List<Tutorial> { new() { Id = "t1" } };
        _tutorialMock.Setup(t => t.GetAllAsync(null)).ReturnsAsync(tutorials);

        DashboardViewModel sut = CreateSut(useNull: true);
        await sut.LoadCommand.ExecuteAsync(null);

        sut.TutorialList.Should().HaveCount(1);
        sut.ProgressList.Should().BeEmpty();
        sut.Achievements.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadStandardsCommand_PopulatesStandards()
    {
        var standards = new List<CompanyStandard>
        {
            new() { Id = "s1", Name = "Rule 1" },
            new() { Id = "s2", Name = "Rule 2" }
        };
        _standardsMock.Setup(s => s.GetStandardsAsync("c1")).ReturnsAsync(standards);

        DashboardViewModel sut = CreateSut();
        await sut.LoadStandardsCommand.ExecuteAsync(null);

        sut.Standards.Should().HaveCount(2);
    }

    [Fact]
    public async Task RunValidationCommand_PopulatesCorrections()
    {
        var corrections = new List<CorrectionEvent>
        {
            new() { Title = "Error 1", IsFixed = false },
            new() { Title = "Error 2", IsFixed = true }
        };
        _standardsMock.Setup(s => s.ValidateModelAsync()).ReturnsAsync(corrections);

        DashboardViewModel sut = CreateSut();
        await sut.RunValidationCommand.ExecuteAsync(null);

        sut.Corrections.Should().HaveCount(2);
        sut.ActiveCorrections.Should().Be(1);
    }

    [Fact]
    public async Task LogoutCommand_DelegatesToAuthService()
    {
        DashboardViewModel sut = CreateSut();
        await sut.LogoutCommand.ExecuteAsync(null);

        _authMock.Verify(a => a.LogoutAsync(), Times.Once);
    }

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        DashboardViewModel sut = CreateSut();
        Action act = () => sut.Dispose();
        act.Should().NotThrow();
    }
}
