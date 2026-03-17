using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BIMConcierge.Core.Interfaces;
using BIMConcierge.Core.Models;
using BIMConcierge.Infrastructure.Revit;

namespace BIMConcierge.UI.ViewModels;

public partial class TutorialViewModel : ObservableObject, IDisposable
{
    private readonly ITutorialService       _service;
    private readonly IAuthService           _auth;
    private readonly IRevitEventDispatcher  _dispatcher;
    private readonly INavigationService    _navigation;

    private CancellationTokenSource _cts = new();

    [ObservableProperty] private Tutorial?      tutorial;
    [ObservableProperty] private TutorialStep?  currentStep;
    [ObservableProperty] private int            currentStepIndex;
    [ObservableProperty] private double         progressPercent;
    [ObservableProperty] private bool           isCompleted;
    [ObservableProperty] private bool           isBusy;

    /// <summary>"Passo 2 de 8"</summary>
    public string StepLabel => $"Passo {CurrentStepIndex + 1} de {Tutorial?.StepCount ?? 0}";

    /// <summary>"35%"</summary>
    public string ProgressLabel => $"{ProgressPercent:0}%";

    /// <summary>Computed: "Categoria" badge text (e.g. "ARQUITETURA").</summary>
    public string CategoryLabel => Tutorial?.Category?.ToUpperInvariant() ?? string.Empty;

    /// <summary>Computed: "INTERMEDIÁRIO" difficulty badge.</summary>
    public string DifficultyLabel => Tutorial?.Difficulty?.ToUpperInvariant() ?? string.Empty;

    /// <summary>Computed: "45 MINUTOS".</summary>
    public string DurationLabel => $"{Tutorial?.DurationMins ?? 0} MINUTOS";

    /// <summary>Computed: "8 PASSOS".</summary>
    public string StepCountLabel => $"{Tutorial?.StepCount ?? 0} PASSOS";

    [ObservableProperty] private int     completedSteps;
    [ObservableProperty] private double  savedProgressPercent;

    public TutorialViewModel(ITutorialService service, IAuthService auth, IRevitEventDispatcher dispatcher, INavigationService navigation)
    {
        _service    = service;
        _auth       = auth;
        _dispatcher = dispatcher;
        _navigation = navigation;
    }

    partial void OnCurrentStepIndexChanged(int value)
    {
        OnPropertyChanged(nameof(StepLabel));
    }

    partial void OnProgressPercentChanged(double value)
    {
        OnPropertyChanged(nameof(ProgressLabel));
    }

    partial void OnTutorialChanged(Tutorial? value)
    {
        OnPropertyChanged(nameof(StepLabel));
        OnPropertyChanged(nameof(CategoryLabel));
        OnPropertyChanged(nameof(DifficultyLabel));
        OnPropertyChanged(nameof(DurationLabel));
        OnPropertyChanged(nameof(StepCountLabel));
    }

    public async Task LoadTutorialAsync(string tutorialId)
    {
        CancelPending();
        var ct = _cts.Token;

        Tutorial = await _service.GetByIdAsync(tutorialId);
        if (Tutorial is null) return;

        ct.ThrowIfCancellationRequested();

        // Restore saved progress
        var userId   = _auth.CurrentUser?.Id ?? string.Empty;
        var progress = await _service.GetProgressAsync(userId, tutorialId);
        CompletedSteps       = progress?.CurrentStep ?? 0;
        SavedProgressPercent = progress?.ProgressPercent ?? 0;
        CurrentStepIndex     = CompletedSteps;
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
        try
        {
            var userId = _auth.CurrentUser?.Id ?? string.Empty;
            await _service.CompleteStepAsync(userId, Tutorial.Id, CurrentStepIndex);
            if (CurrentStepIndex >= Tutorial.StepCount - 1)
                IsCompleted = true;
            else
                GoToStep(CurrentStepIndex + 1);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void PreviousStep()
    {
        if (CurrentStepIndex > 0) GoToStep(CurrentStepIndex - 1);
    }

    [RelayCommand]
    private void ApplyAutoFix()
    {
        if (CurrentStep?.RevitCommand is null || Tutorial is null) return;

        // Dispatch auto-fix through the Revit event dispatcher
        // The RevitEventBridge handles the actual Revit API transaction
        if (_dispatcher is RevitEventDispatcher concrete)
        {
            var elements = new List<(string ElementId, string Name, string Category)>
            {
                ("0", CurrentStep.RevitCommand, Tutorial.Category)
            };
            concrete.ValidateElements(elements);
        }
    }

    /// <summary>Opens the GuidedTutorialWindow for the currently loaded tutorial.</summary>
    [RelayCommand]
    private void StartTutorial()
    {
        if (Tutorial is null) return;
        _navigation.NavigateTo("GuidedTutorial", Tutorial.Id);
    }

    private void CancelPending()
    {
        _cts.Cancel();
        _cts.Dispose();
        _cts = new CancellationTokenSource();
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}
