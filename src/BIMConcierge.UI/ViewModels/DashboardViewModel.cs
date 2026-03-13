using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BIMConcierge.Core.Interfaces;
using BIMConcierge.Core.Models;

namespace BIMConcierge.UI.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly IAuthService      _auth;
    private readonly ITutorialService  _tutorials;
    private readonly IProgressService  _progress;
    private readonly IStandardsService _standards;

    // ── Bound Properties ─────────────────────────────────────────────────────
    [ObservableProperty] private User?   currentUser;
    [ObservableProperty] private string  activeSection = "Dashboard";
    [ObservableProperty] private bool    isBusy;

    // Dashboard summary
    [ObservableProperty] private int  completedTutorials;
    [ObservableProperty] private int  totalTutorials;
    [ObservableProperty] private int  activeCorrections;
    [ObservableProperty] private int  xpPoints;

    // Tutorial library
    [ObservableProperty] private List<Tutorial>         tutorialList  = [];
    [ObservableProperty] private Tutorial?              selectedTutorial;
    [ObservableProperty] private string                 searchQuery   = string.Empty;

    // Corrections
    [ObservableProperty] private List<CorrectionEvent>  corrections   = [];

    // Standards
    [ObservableProperty] private List<CompanyStandard>  standards     = [];

    // Progress
    [ObservableProperty] private List<TutorialProgress> progressList  = [];

    // Achievements
    [ObservableProperty] private List<Achievement>      achievements  = [];

    public DashboardViewModel(
        IAuthService auth, ITutorialService tutorials,
        IProgressService progress, IStandardsService standards)
    {
        _auth       = auth;
        _tutorials  = tutorials;
        _progress   = progress;
        _standards  = standards;
        CurrentUser = auth.CurrentUser;
    }

    // ── Startup ──────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            await Task.WhenAll(LoadTutorialsAsync(), LoadProgressAsync(), LoadAchievementsAsync());
        }
        finally { IsBusy = false; }
    }

    // ── Navigation ───────────────────────────────────────────────────────────

    [RelayCommand] private void NavigateTo(string section) => ActiveSection = section;

    // ── Tutorial Library ─────────────────────────────────────────────────────

    private async Task LoadTutorialsAsync()
    {
        TutorialList     = await _tutorials.GetAllAsync();
        TotalTutorials   = TutorialList.Count;
    }

    [RelayCommand]
    private void SelectTutorial(Tutorial t)
    {
        SelectedTutorial = t;
        ActiveSection    = "TutorialDetail";
    }

    // ── Progress ─────────────────────────────────────────────────────────────

    private async Task LoadProgressAsync()
    {
        if (CurrentUser is null) return;
        ProgressList         = await _progress.GetUserProgressAsync(CurrentUser.Id);
        CompletedTutorials   = ProgressList.Count(p => p.IsCompleted);
    }

    // ── Achievements ─────────────────────────────────────────────────────────

    private async Task LoadAchievementsAsync()
    {
        if (CurrentUser is null) return;
        Achievements = await _progress.GetAchievementsAsync(CurrentUser.Id);
        XpPoints     = CurrentUser.XpPoints;
    }

    // ── Standards ────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task LoadStandardsAsync()
    {
        if (CurrentUser is null) return;
        Standards = await _standards.GetStandardsAsync(CurrentUser.CompanyId);
    }

    [RelayCommand]
    private async Task RunValidationAsync()
    {
        IsBusy      = true;
        Corrections = await _standards.ValidateModelAsync();
        ActiveCorrections = Corrections.Count(c => !c.IsFixed);
        IsBusy      = false;
    }

    [RelayCommand]
    private async Task AutoFixAsync(CorrectionEvent ev)
    {
        if (await _standards.AutoFixAsync(ev.Id))
            ev.IsFixed = true;
    }

    // ── Logout ───────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task LogoutAsync() => await _auth.LogoutAsync();
}
