using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BIMConcierge.Core.Interfaces;
using BIMConcierge.Core.Models;
using BIMConcierge.UI.Localization;

namespace BIMConcierge.UI.ViewModels;

public partial class TutorialLibraryViewModel : ObservableObject, IDisposable
{
    private readonly ITutorialService   _tutorials;
    private readonly IProgressService   _progress;
    private readonly IAuthService       _auth;
    private readonly INavigationService _navigation;

    private CancellationTokenSource _cts = new();

    [ObservableProperty] private bool   _isBusy;
    [ObservableProperty] private string _errorMessage    = string.Empty;
    [ObservableProperty] private string _searchQuery     = string.Empty;
    [ObservableProperty] private string _selectedCategory = "Todos";

    // User profile for sidebar
    [ObservableProperty] private string _userInitials = string.Empty;
    [ObservableProperty] private string _userName     = string.Empty;
    [ObservableProperty] private string _userRole     = string.Empty;

    /// <summary>All tutorials loaded from the API (unfiltered).</summary>
    private readonly List<Tutorial> _allTutorials = [];

    /// <summary>Filtered tutorials bound to the UI card grid.</summary>
    public ObservableCollection<Tutorial> Tutorials { get; } = [];

    /// <summary>Progress keyed by TutorialId for card display.</summary>
    public Dictionary<string, TutorialProgress> ProgressMap { get; } = [];

    public TutorialLibraryViewModel(
        ITutorialService tutorials,
        IProgressService progress,
        IAuthService auth,
        INavigationService navigation)
    {
        _tutorials  = tutorials;
        _progress   = progress;
        _auth       = auth;
        _navigation = navigation;

        User? user = auth.CurrentUser;
        if (user is not null)
        {
            UserInitials = user.Initials;
            UserName     = user.Name;
            UserRole     = user.Role;
        }
    }

    // ── Load ────────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task LoadAsync()
    {
        CancelPending();
        CancellationToken ct = _cts.Token;

        IsBusy = true;
        try
        {
            ErrorMessage = string.Empty;

            Task<List<Tutorial>> tutorialsTask = _tutorials.GetAllAsync();
            string userId = _auth.CurrentUser?.Id ?? string.Empty;
            Task<List<TutorialProgress>> progressTask = string.IsNullOrEmpty(userId)
                ? Task.FromResult(new List<TutorialProgress>())
                : _progress.GetUserProgressAsync(userId);

            await Task.WhenAll(tutorialsTask, progressTask);
            ct.ThrowIfCancellationRequested();

            _allTutorials.Clear();
            _allTutorials.AddRange(await tutorialsTask);

            ProgressMap.Clear();
            foreach (TutorialProgress p in await progressTask)
                ProgressMap[p.TutorialId] = p;

            ApplyFilter();
        }
        catch (OperationCanceledException) { /* expected */ }
        catch (Exception ex)
        {
            ErrorMessage = TranslationSource.Format("TutorialsLoadError", ex.Message);
        }
        finally { IsBusy = false; }
    }

    // ── Filter ──────────────────────────────────────────────────────────────

    [RelayCommand]
    private void SetFilter(string category)
    {
        SelectedCategory = category;
        ApplyFilter();
    }

    partial void OnSearchQueryChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        IEnumerable<Tutorial> filtered = _allTutorials.AsEnumerable();

        if (!string.IsNullOrEmpty(SelectedCategory) && SelectedCategory != "Todos")
        {
            filtered = filtered.Where(t =>
                t.Category.Contains(SelectedCategory, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            string query = SearchQuery;
            filtered = filtered.Where(t =>
                t.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                t.Description.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        Tutorials.Clear();
        foreach (Tutorial t in filtered)
            Tutorials.Add(t);
    }

    // ── Navigation ──────────────────────────────────────────────────────────

    [RelayCommand]
    private void OpenTutorial(Tutorial tutorial)
    {
        _navigation.NavigateTo("TutorialDetail", tutorial.Id);
    }

    [RelayCommand]
    private void OpenWindow(string windowName) => _navigation.NavigateTo(windowName);

    // ── Helpers ─────────────────────────────────────────────────────────────

    public TutorialProgress? GetProgress(string tutorialId)
    {
        ProgressMap.TryGetValue(tutorialId, out TutorialProgress? p);
        return p;
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
