using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BIMConcierge.Core.Interfaces;
using BIMConcierge.Core.Models;

namespace BIMConcierge.UI.ViewModels;

public partial class StudentProgressViewModel : ObservableObject, IDisposable
{
    private readonly IProgressService    _progress;
    private readonly ITutorialService    _tutorials;
    private readonly IAuthService        _auth;
    private readonly INavigationService  _navigation;

    private CancellationTokenSource _cts = new();

    [ObservableProperty] private bool   isBusy;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private int    completedCount;
    [ObservableProperty] private int    totalCount;
    [ObservableProperty] private int    xpPoints;
    [ObservableProperty] private int    learningHours;
    [ObservableProperty] private int    certificateCount;
    [ObservableProperty] private string userName = string.Empty;
    [ObservableProperty] private string userRole = string.Empty;
    [ObservableProperty] private int    userLevel;
    [ObservableProperty] private double nextLevelPercent;

    public ObservableCollection<TutorialProgress> InProgressTutorials { get; } = [];
    public ObservableCollection<Achievement>      RecentAchievements  { get; } = [];
    public ObservableCollection<SkillProficiency>  Skills             { get; } = [];

    public StudentProgressViewModel(
        IProgressService progress, ITutorialService tutorials,
        IAuthService auth, INavigationService navigation)
    {
        _progress   = progress;
        _tutorials  = tutorials;
        _auth       = auth;
        _navigation = navigation;

        var user = auth.CurrentUser;
        if (user is not null)
        {
            UserName  = user.Name;
            UserRole  = user.Role;
            UserLevel = user.Level;
            XpPoints  = user.XpPoints;
        }
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        CancelPending();
        var ct = _cts.Token;

        IsBusy = true;
        try
        {
            ErrorMessage = string.Empty;
            var userId = _auth.CurrentUser?.Id ?? string.Empty;

            var progressTask     = _progress.GetUserProgressAsync(userId);
            var achievementsTask = _progress.GetAchievementsAsync(userId);
            var tutorialsTask    = _tutorials.GetAllAsync();

            await Task.WhenAll(progressTask, achievementsTask, tutorialsTask);
            ct.ThrowIfCancellationRequested();

            var allProgress = progressTask.Result;
            var achievements = achievementsTask.Result;
            var tutorials = tutorialsTask.Result;

            // Stats
            CompletedCount   = allProgress.Count(p => p.IsCompleted);
            TotalCount       = tutorials.Count;
            LearningHours    = (int)allProgress.Sum(p => p.TotalSteps * 0.5); // ~30min per step
            CertificateCount = allProgress.Count(p => p.IsCompleted && p.ScorePercent >= 80);

            // In-progress tutorials
            InProgressTutorials.Clear();
            foreach (TutorialProgress p in allProgress.Where(p => !p.IsCompleted))
                InProgressTutorials.Add(p);

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
            ErrorMessage = $"Erro ao carregar progresso: {ex.Message}";
        }
        finally { IsBusy = false; }
    }

    private void CalculateSkillProficiency(List<Tutorial> tutorials, List<TutorialProgress> allProgress)
    {
        Skills.Clear();

        // Group tutorials by category, then calculate average progress per category
        var progressByTutorial = allProgress.ToDictionary(p => p.TutorialId);
        var categoryGroups = tutorials.GroupBy(t => t.Category);

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
