using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BIMConcierge.Core.Interfaces;
using BIMConcierge.Core.Models;

namespace BIMConcierge.UI.ViewModels;

public partial class DashboardViewModel : ObservableObject, IDisposable
{
    private readonly IAuthService        _auth;
    private readonly ITutorialService    _tutorials;
    private readonly IProgressService    _progress;
    private readonly IStandardsService   _standards;
    private readonly INavigationService  _navigation;

    private CancellationTokenSource _cts = new();

    // ── Bound Properties ─────────────────────────────────────────────────────
    [ObservableProperty] private User?   _currentUser;
    [ObservableProperty] private string  _activeSection = "Dashboard";
    [ObservableProperty] private bool    _isBusy;

    // Dashboard summary
    [ObservableProperty] private int  _completedTutorials;
    [ObservableProperty] private int  _totalTutorials;
    [ObservableProperty] private int  _activeCorrections;
    [ObservableProperty] private int  _xpPoints;

    // Tutorial library
    [ObservableProperty] private Tutorial?  _selectedTutorial;
    [ObservableProperty] private string     _searchQuery = string.Empty;

    public ObservableCollection<Tutorial>         TutorialList  { get; } = [];
    public ObservableCollection<CorrectionEvent>  Corrections   { get; } = [];
    public ObservableCollection<CompanyStandard>  Standards     { get; } = [];
    public ObservableCollection<TutorialProgress> ProgressList  { get; } = [];
    public ObservableCollection<Achievement>      Achievements  { get; } = [];

    public DashboardViewModel(
        IAuthService auth, ITutorialService tutorials,
        IProgressService progress, IStandardsService standards,
        INavigationService navigation)
    {
        _auth       = auth;
        _tutorials  = tutorials;
        _progress   = progress;
        _standards  = standards;
        _navigation = navigation;
        CurrentUser = auth.CurrentUser;
    }

    // ── Startup ──────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task LoadAsync()
    {
        CancelPending();
        CancellationToken ct = _cts.Token;

        IsBusy = true;
        try
        {
            await Task.WhenAll(
                LoadTutorialsAsync(ct),
                LoadProgressAsync(ct),
                LoadAchievementsAsync(ct));
        }
        finally { IsBusy = false; }
    }

    // ── Navigation ───────────────────────────────────────────────────────────

    [RelayCommand] private void NavigateTo(string section) => ActiveSection = section;

    /// <summary>Opens a window via the navigation service. Called from sidebar buttons.</summary>
    [RelayCommand]
    private void OpenWindow(string windowName) => _navigation.NavigateTo(windowName);

    // ── Tutorial Library ─────────────────────────────────────────────────────

    private async Task LoadTutorialsAsync(CancellationToken ct)
    {
        List<Tutorial> list = await _tutorials.GetAllAsync();
        ct.ThrowIfCancellationRequested();

        TutorialList.Clear();
        foreach (Tutorial t in list) TutorialList.Add(t);
        TotalTutorials = TutorialList.Count;
    }

    [RelayCommand]
    private void SelectTutorial(Tutorial t)
    {
        SelectedTutorial = t;
        ActiveSection = "TutorialDetail";
        _navigation.NavigateTo("TutorialDetail", t.Id);
    }

    [RelayCommand]
    private void OpenGuidedTutorial(Tutorial t)
    {
        _navigation.NavigateTo("GuidedTutorial", t.Id);
    }

    // ── Progress ─────────────────────────────────────────────────────────────

    private async Task LoadProgressAsync(CancellationToken ct)
    {
        if (CurrentUser is null) return;
        List<TutorialProgress> list = await _progress.GetUserProgressAsync(CurrentUser.Id);
        ct.ThrowIfCancellationRequested();

        ProgressList.Clear();
        foreach (TutorialProgress p in list) ProgressList.Add(p);
        CompletedTutorials = ProgressList.Count(p => p.IsCompleted);
    }

    // ── Achievements ─────────────────────────────────────────────────────────

    private async Task LoadAchievementsAsync(CancellationToken ct)
    {
        if (CurrentUser is null) return;
        List<Achievement> list = await _progress.GetAchievementsAsync(CurrentUser.Id);
        ct.ThrowIfCancellationRequested();

        Achievements.Clear();
        foreach (Achievement a in list) Achievements.Add(a);
        XpPoints = CurrentUser.XpPoints;
    }

    // ── Standards ────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task LoadStandardsAsync()
    {
        if (CurrentUser is null) return;
        List<CompanyStandard> list = await _standards.GetStandardsAsync(CurrentUser.CompanyId);

        Standards.Clear();
        foreach (CompanyStandard s in list) Standards.Add(s);
    }

    [RelayCommand]
    private async Task RunValidationAsync()
    {
        IsBusy = true;
        List<CorrectionEvent> list = await _standards.ValidateModelAsync();

        Corrections.Clear();
        foreach (CorrectionEvent c in list) Corrections.Add(c);
        ActiveCorrections = Corrections.Count(c => !c.IsFixed);
        IsBusy = false;
    }

    [RelayCommand]
    private async Task AutoFixAsync(CorrectionEvent ev)
    {
        if (await _standards.AutoFixAsync(ev.Id))
            ev.IsFixed = true;
    }

    // ── Logout ───────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task LogoutAsync()
    {
        CancelPending();
        await _auth.LogoutAsync();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

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
