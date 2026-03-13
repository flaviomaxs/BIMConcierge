using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BIMConcierge.Core.Interfaces;
using BIMConcierge.Core.Models;

namespace BIMConcierge.UI.ViewModels;

public partial class TutorialViewModel : ObservableObject
{
    private readonly ITutorialService _service;
    private readonly IAuthService     _auth;

    [ObservableProperty] private Tutorial?      tutorial;
    [ObservableProperty] private TutorialStep?  currentStep;
    [ObservableProperty] private int            currentStepIndex;
    [ObservableProperty] private double         progressPercent;
    [ObservableProperty] private bool           isCompleted;
    [ObservableProperty] private bool           isBusy;

    public TutorialViewModel(ITutorialService service, IAuthService auth)
    {
        _service = service;
        _auth    = auth;
    }

    public async Task LoadTutorialAsync(string tutorialId)
    {
        Tutorial = await _service.GetByIdAsync(tutorialId);
        if (Tutorial is null) return;

        // Restore saved progress
        var userId   = _auth.CurrentUser?.Id ?? string.Empty;
        var progress = await _service.GetProgressAsync(userId, tutorialId);
        CurrentStepIndex = progress?.CurrentStep ?? 0;
        GoToStep(CurrentStepIndex);
    }

    private void GoToStep(int index)
    {
        if (Tutorial is null) return;
        CurrentStepIndex = Math.Clamp(index, 0, Tutorial.StepCount - 1);
        CurrentStep      = Tutorial.Steps.ElementAtOrDefault(CurrentStepIndex);
        ProgressPercent  = Tutorial.StepCount > 0
            ? (double)(CurrentStepIndex + 1) / Tutorial.StepCount * 100
            : 0;
    }

    [RelayCommand]
    private async Task NextStepAsync()
    {
        if (Tutorial is null) return;
        IsBusy = true;
        var userId = _auth.CurrentUser?.Id ?? string.Empty;
        await _service.CompleteStepAsync(userId, Tutorial.Id, CurrentStepIndex);
        if (CurrentStepIndex >= Tutorial.StepCount - 1)
            IsCompleted = true;
        else
            GoToStep(CurrentStepIndex + 1);
        IsBusy = false;
    }

    [RelayCommand]
    private void PreviousStep()
    {
        if (CurrentStepIndex > 0) GoToStep(CurrentStepIndex - 1);
    }

    [RelayCommand]
    private void ApplyAutoFix()
    {
        // TODO: dispatch Revit external event to run CurrentStep.RevitCommand
    }
}
