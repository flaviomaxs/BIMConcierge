using BIMConcierge.Core.Interfaces;
using BIMConcierge.Core.Models;
using BIMConcierge.Infrastructure.Revit;
using BIMConcierge.UI.ViewModels;
using FluentAssertions;
using Moq;
using Xunit;

namespace BIMConcierge.Core.Tests;

public class TutorialViewModelTests
{
    private readonly Mock<ITutorialService> _tutorialMock = new();
    private readonly Mock<IAuthService> _authMock = new();
    private readonly Mock<IRevitEventDispatcher> _dispatcherMock = new();
    private readonly Mock<INavigationService> _navigationMock = new();

    private static readonly User TestUser = new() { Id = "u1", Name = "Test User" };

    private TutorialViewModel CreateSut()
    {
        _authMock.SetupGet(a => a.CurrentUser).Returns(TestUser);
        return new TutorialViewModel(_tutorialMock.Object, _authMock.Object, _dispatcherMock.Object, _navigationMock.Object);
    }

    private static Tutorial CreateTestTutorial(int stepCount = 3) => new()
    {
        Id = "t1",
        Title = "Test Tutorial",
        Category = "Walls",
        StepCount = stepCount,
        Steps =
        [
            new TutorialStep { Order = 0, Title = "Step 1", Instruction = "Do step 1" },
            new TutorialStep { Order = 1, Title = "Step 2", Instruction = "Do step 2" },
            new TutorialStep { Order = 2, Title = "Step 3", Instruction = "Do step 3" }
        ]
    };

    [Fact]
    public void InitialState_PropertiesAreDefault()
    {
        TutorialViewModel sut = CreateSut();

        sut.Tutorial.Should().BeNull();
        sut.CurrentStep.Should().BeNull();
        sut.CurrentStepIndex.Should().Be(0);
        sut.ProgressPercent.Should().Be(0);
        sut.IsCompleted.Should().BeFalse();
        sut.IsBusy.Should().BeFalse();
    }

    [Fact]
    public async Task LoadTutorialAsync_ValidId_LoadsTutorialAndSetsFirstStep()
    {
        Tutorial tutorial = CreateTestTutorial();
        _tutorialMock.Setup(s => s.GetByIdAsync("t1")).ReturnsAsync(tutorial);
        _tutorialMock.Setup(s => s.GetProgressAsync("u1", "t1")).ReturnsAsync((TutorialProgress?)null);

        TutorialViewModel sut = CreateSut();
        await sut.LoadTutorialAsync("t1");

        sut.Tutorial.Should().NotBeNull();
        sut.CurrentStep.Should().NotBeNull();
        sut.CurrentStep!.Title.Should().Be("Step 1");
        sut.ProgressPercent.Should().BeApproximately(33.33, 0.1);
    }

    [Fact]
    public async Task LoadTutorialAsync_WithSavedProgress_RestoresStep()
    {
        Tutorial tutorial = CreateTestTutorial();
        var savedProgress = new TutorialProgress { UserId = "u1", TutorialId = "t1", CurrentStep = 2 };

        _tutorialMock.Setup(s => s.GetByIdAsync("t1")).ReturnsAsync(tutorial);
        _tutorialMock.Setup(s => s.GetProgressAsync("u1", "t1")).ReturnsAsync(savedProgress);

        TutorialViewModel sut = CreateSut();
        await sut.LoadTutorialAsync("t1");

        sut.CurrentStepIndex.Should().Be(2);
        sut.CurrentStep!.Title.Should().Be("Step 3");
    }

    [Fact]
    public async Task LoadTutorialAsync_NullResult_DoesNotSetStep()
    {
        _tutorialMock.Setup(s => s.GetByIdAsync("t1")).ReturnsAsync((Tutorial?)null);

        TutorialViewModel sut = CreateSut();
        await sut.LoadTutorialAsync("t1");

        sut.Tutorial.Should().BeNull();
        sut.CurrentStep.Should().BeNull();
    }

    [Fact]
    public async Task NextStepCommand_AdvancesToNextStep()
    {
        Tutorial tutorial = CreateTestTutorial();
        _tutorialMock.Setup(s => s.GetByIdAsync("t1")).ReturnsAsync(tutorial);
        _tutorialMock.Setup(s => s.GetProgressAsync("u1", "t1")).ReturnsAsync((TutorialProgress?)null);
        _tutorialMock.Setup(s => s.CompleteStepAsync("u1", "t1", 0)).ReturnsAsync(true);

        TutorialViewModel sut = CreateSut();
        await sut.LoadTutorialAsync("t1");
        await sut.NextStepCommand.ExecuteAsync(null);

        sut.CurrentStepIndex.Should().Be(1);
        sut.CurrentStep!.Title.Should().Be("Step 2");
    }

    [Fact]
    public async Task NextStepCommand_AtLastStep_MarksCompleted()
    {
        Tutorial tutorial = CreateTestTutorial();
        _tutorialMock.Setup(s => s.GetByIdAsync("t1")).ReturnsAsync(tutorial);
        _tutorialMock.Setup(s => s.GetProgressAsync("u1", "t1"))
            .ReturnsAsync(new TutorialProgress { CurrentStep = 2 });
        _tutorialMock.Setup(s => s.CompleteStepAsync("u1", "t1", 2)).ReturnsAsync(true);

        TutorialViewModel sut = CreateSut();
        await sut.LoadTutorialAsync("t1");
        await sut.NextStepCommand.ExecuteAsync(null);

        sut.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public void PreviousStepCommand_AtFirstStep_DoesNothing()
    {
        TutorialViewModel sut = CreateSut();
        sut.PreviousStepCommand.Execute(null);

        sut.CurrentStepIndex.Should().Be(0);
    }

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        TutorialViewModel sut = CreateSut();
        Action act = () => sut.Dispose();
        act.Should().NotThrow();
    }
}
