using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BIMConcierge.Core.Interfaces;
using BIMConcierge.Core.Models;
using BIMConcierge.UI.Localization;

namespace BIMConcierge.UI.ViewModels;

public partial class StudentProgressViewModel : ObservableObject, IDisposable
{
    private readonly IProgressService    _progress;
    private readonly ITutorialService    _tutorials;
    private readonly IAuthService        _auth;
    private readonly INavigationService  _navigation;

    private CancellationTokenSource _cts = new();

    [ObservableProperty] private bool   _isBusy;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private int    _completedCount;
    [ObservableProperty] private int    _totalCount;
    [ObservableProperty] private int    _xpPoints;
    [ObservableProperty] private int    _learningHours;
    [ObservableProperty] private int    _certificateCount;
    [ObservableProperty] private string _userInitials = string.Empty;
    [ObservableProperty] private string _userName = string.Empty;
    [ObservableProperty] private string _userRole = string.Empty;
    [ObservableProperty] private int    _userLevel;
    [ObservableProperty] private double _nextLevelPercent;

    public ObservableCollection<TutorialProgress> InProgressTutorials   { get; } = [];
    public ObservableCollection<Tutorial>         RecommendedTutorials { get; } = [];
    public ObservableCollection<Achievement>      RecentAchievements   { get; } = [];
    public ObservableCollection<SkillProficiency>  Skills              { get; } = [];

    public StudentProgressViewModel(
        IProgressService progress, ITutorialService tutorials,
        IAuthService auth, INavigationService navigation)
    {
        _progress   = progress;
        _tutorials  = tutorials;
        _auth       = auth;
        _navigation = navigation;

        User? user = auth.CurrentUser;
        if (user is not null)
        {
            UserInitials = user.Initials;
            UserName     = user.Name;
            UserRole     = user.Role;
            UserLevel    = user.Level;
            XpPoints     = user.XpPoints;
        }
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        CancelPending();
        CancellationToken ct = _cts.Token;

        IsBusy = true;
        try
        {
            ErrorMessage = string.Empty;
            string userId = _auth.CurrentUser?.Id ?? string.Empty;

            Task<List<TutorialProgress>> progressTask     = _progress.GetUserProgressAsync(userId);
            Task<List<Achievement>> achievementsTask = _progress.GetAchievementsAsync(userId);
            Task<List<Tutorial>> tutorialsTask    = _tutorials.GetAllAsync();

            await Task.WhenAll(progressTask, achievementsTask, tutorialsTask);
            ct.ThrowIfCancellationRequested();

            List<TutorialProgress> allProgress = progressTask.Result;
            List<Achievement> achievements = achievementsTask.Result;
            List<Tutorial> tutorials = tutorialsTask.Result;

            // Stats
            CompletedCount   = allProgress.Count(p => p.IsCompleted);
            TotalCount       = tutorials.Count;
            LearningHours    = (int)allProgress.Sum(p => p.TotalSteps * 0.5); // ~30min per step
            CertificateCount = allProgress.Count(p => p.IsCompleted && p.ScorePercent >= 80);

            // In-progress tutorials (populate titles from tutorial list)
            Dictionary<string, string> tutorialTitles = tutorials.ToDictionary(t => t.Id, t => t.Title);
            InProgressTutorials.Clear();
            foreach (TutorialProgress p in allProgress.Where(p => !p.IsCompleted))
            {
                if (tutorialTitles.TryGetValue(p.TutorialId, out string? title))
                    p.TutorialTitle = title;
                InProgressTutorials.Add(p);
            }

            // Recommended tutorials (not started yet)
            HashSet<string> startedIds = allProgress.Select(p => p.TutorialId).ToHashSet();
            RecommendedTutorials.Clear();
            foreach (Tutorial t in tutorials.Where(t => !startedIds.Contains(t.Id)).Take(4))
                RecommendedTutorials.Add(t);

            // Recent achievements (unlocked)
            RecentAchievements.Clear();
            foreach (Achievement a in achievements.Where(a => a.IsUnlocked).OrderByDescending(a => a.UnlockedAt).Take(5))
                RecentAchievements.Add(a);

            // Skill proficiency — calculated from tutorial progress grouped by category
            CalculateSkillProficiency(tutorials, allProgress);

            // Level progress
            NextLevelPercent = CalculateLevelProgress();
        }
        catch (Exception ex)
        {
            ErrorMessage = TranslationSource.Format("ProgressLoadError", ex.Message);
        }
        finally { IsBusy = false; }
    }

    private void CalculateSkillProficiency(List<Tutorial> tutorials, List<TutorialProgress> allProgress)
    {
        Skills.Clear();

        // Group tutorials by category, then calculate average progress per category
        Dictionary<string, TutorialProgress> progressByTutorial = allProgress.ToDictionary(p => p.TutorialId);
        IEnumerable<IGrouping<string, Tutorial>> categoryGroups = tutorials.GroupBy(t => t.Category);

        foreach (IGrouping<string, Tutorial> group in categoryGroups)
        {
            if (string.IsNullOrWhiteSpace(group.Key)) continue;

            int totalSteps = 0;
            int completedSteps = 0;

            foreach (Tutorial tutorial in group)
            {
                totalSteps += tutorial.StepCount;
                if (progressByTutorial.TryGetValue(tutorial.Id, out TutorialProgress? progress))
                    completedSteps += progress.CurrentStep;
            }

            int percent = totalSteps > 0 ? (int)((double)completedSteps / totalSteps * 100) : 0;
            Skills.Add(new SkillProficiency { Name = group.Key, Percent = Math.Clamp(percent, 0, 100) });
        }
    }

    [RelayCommand]
    private void OpenWindow(string windowName) => _navigation.NavigateTo(windowName);

    private double CalculateLevelProgress()
    {
        const int xpPerLevel = 1000;
        int xpInCurrentLevel = XpPoints % xpPerLevel;
        return (double)xpInCurrentLevel / xpPerLevel * 100;
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

/// <summary>Represents a skill proficiency bar for display.</summary>
public class SkillProficiency
{
    public string Name    { get; set; } = string.Empty;
    public int    Percent { get; set; }
}
