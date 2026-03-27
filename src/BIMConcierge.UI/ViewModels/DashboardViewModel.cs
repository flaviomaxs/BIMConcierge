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

    private CancellationTokenSource _cts = new();

    // ── Bound Properties ─────────────────────────────────────────────────────
    [ObservableProperty] private User?   _currentUser;
    [ObservableProperty] private string  _activeSection = "Dashboard";
    [ObservableProperty] private bool    _isBusy;
    [ObservableProperty] private bool    _sessionExpired;

    // Login state — controls overlay visibility
    [ObservableProperty] private bool _isLoggedIn;

    // Overlay states
    [ObservableProperty] private bool _isGuidedTutorialOpen;
    [ObservableProperty] private bool _isCorrectionAlertVisible;
    [ObservableProperty] private CorrectionEvent? _currentCorrectionAlert;
    [ObservableProperty] private string? _guidedTutorialId;

    // User profile
    [ObservableProperty] private string _userInitials = string.Empty;
    [ObservableProperty] private string _userName     = string.Empty;
    [ObservableProperty] private string _userRole     = string.Empty;

    // Dashboard summary
    [ObservableProperty] private int  _completedTutorials;
    [ObservableProperty] private int  _totalTutorials;
    [ObservableProperty] private int  _activeCorrections;
    [ObservableProperty] private int  _xpPoints;
    [ObservableProperty] private int  _compliancePercent;

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
        IProgressService progress, IStandardsService standards)
    {
        _auth       = auth;
        _tutorials  = tutorials;
        _progress   = progress;
        _standards  = standards;

        // Check if already authenticated (e.g. persisted session)
        IsLoggedIn = auth.IsAuthenticated;
        if (IsLoggedIn)
            SetUserProfile();
    }

    private void SetUserProfile()
    {
        CurrentUser  = _auth.CurrentUser;
        UserInitials = _auth.CurrentUser?.Initials ?? string.Empty;
        UserName     = _auth.CurrentUser?.Name ?? string.Empty;
        UserRole     = _auth.CurrentUser?.Role ?? string.Empty;
    }

    // ── Login Success ──────────────────────────────────────────────────────

    /// <summary>
    /// Called by DashboardWindow when LoginSectionView reports successful login.
    /// </summary>
    public void OnLoginSucceeded()
    {
        IsLoggedIn = true;

        try { SetUserProfile(); }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "SetUserProfile failed after login");
        }

        _ = LoadDataAsync(skipSessionCheck: true);
    }

    // ── Startup ──────────────────────────────────────────────────────────────

    [RelayCommand]
    private Task LoadAsync() => LoadDataAsync(skipSessionCheck: false);

    private async Task LoadDataAsync(bool skipSessionCheck)
    {
        CancelPending();
        CancellationToken ct = _cts.Token;

        IsBusy = true;
        try
        {
            // Revalidate session (token expiry + license)
            // Skip when called right after login — the token was just issued.
            if (!skipSessionCheck && !await _auth.EnsureValidSessionAsync())
            {
                await _auth.LogoutAsync();
                SessionExpired = true;
                IsLoggedIn = false;
                return;
            }

            // Load company standards into the rule engine
            if (CurrentUser is not null && !string.IsNullOrEmpty(CurrentUser.CompanyId))
            {
                try { await _standards.GetStandardsAsync(CurrentUser.CompanyId); }
                catch { /* best-effort — don't block dashboard */ }
            }

            await Task.WhenAll(
                LoadTutorialsAsync(ct),
                LoadProgressAsync(ct),
                LoadAchievementsAsync(ct));
        }
        finally { IsBusy = false; }
    }

    // ── Navigation ───────────────────────────────────────────────────────────

    [RelayCommand] private void NavigateTo(string section) => ActiveSection = section;

    // ── Guided Tutorial Overlay ──────────────────────────────────────────────

    [RelayCommand]
    private void OpenGuidedTutorial(Tutorial t)
    {
        GuidedTutorialId = t.Id;
        IsGuidedTutorialOpen = true;
    }

    [RelayCommand]
    private void CloseGuidedTutorial()
    {
        IsGuidedTutorialOpen = false;
        GuidedTutorialId = null;
    }

    // ── Correction Alert Overlay ─────────────────────────────────────────────

    [RelayCommand]
    private void ShowCorrectionAlert(CorrectionEvent ev)
    {
        CurrentCorrectionAlert = ev;
        IsCorrectionAlertVisible = true;
    }

    [RelayCommand]
    private void DismissCorrectionAlert()
    {
        IsCorrectionAlertVisible = false;
        CurrentCorrectionAlert = null;
    }

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
        IsLoggedIn = false;
        ActiveSection = "Dashboard";
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
