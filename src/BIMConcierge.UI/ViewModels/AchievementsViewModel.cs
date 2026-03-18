using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BIMConcierge.Core.Interfaces;
using BIMConcierge.Core.Models;
using BIMConcierge.UI.Localization;

namespace BIMConcierge.UI.ViewModels;

public partial class AchievementsViewModel : ObservableObject, IDisposable
{
    private readonly IProgressService    _progress;
    private readonly IAuthService        _auth;
    private readonly INavigationService  _navigation;

    private CancellationTokenSource _cts = new();

    [ObservableProperty] private bool   isBusy;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private int    userLevel;
    [ObservableProperty] private string levelTitle = string.Empty;
    [ObservableProperty] private int    currentXp;
    [ObservableProperty] private int    xpForNextLevel;
    [ObservableProperty] private double levelProgressPercent;
    [ObservableProperty] private string userName = string.Empty;
    [ObservableProperty] private string filterMode = "All";

    public ObservableCollection<Achievement>     AllAchievements { get; } = [];
    public ObservableCollection<Achievement>     FilteredAchievements { get; } = [];
    public ObservableCollection<QuestItem>       ActiveQuests    { get; } = [];
    public ObservableCollection<LeaderboardEntry> Leaderboard    { get; } = [];
    public ObservableCollection<RewardItem>      Rewards         { get; } = [];

    public AchievementsViewModel(IProgressService progress, IAuthService auth, INavigationService navigation)
    {
        _progress   = progress;
        _auth       = auth;
        _navigation = navigation;

        var user = auth.CurrentUser;
        if (user is not null)
        {
            UserName   = user.Name;
            UserLevel  = user.Level;
            CurrentXp  = user.XpPoints;
            LevelTitle = $"Level {user.Level}";
            CalculateLevelProgress();
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
            List<Achievement> achievements = await _progress.GetAchievementsAsync(userId);
            ct.ThrowIfCancellationRequested();

            AllAchievements.Clear();
            foreach (Achievement a in achievements) AllAchievements.Add(a);

            ApplyFilter();
            LoadQuests(achievements);
            LoadLeaderboard();
            LoadRewards();
        }
        catch (Exception ex)
        {
            ErrorMessage = TranslationSource.Format("AchievementsLoadError", ex.Message);
        }
        finally { IsBusy = false; }
    }

    private void LoadQuests(List<Achievement> achievements)
    {
        ActiveQuests.Clear();

        // Quest: fix naming errors (based on unlocked standards-related achievements)
        int standardsFixed = achievements.Count(a => a.IsUnlocked && a.Title.Contains("Standard", StringComparison.OrdinalIgnoreCase));
        ActiveQuests.Add(new QuestItem
        {
            Title = "Guardião de Padrões",
            Description = "Corrija 10 erros de nomenclatura no projeto Revit ativo.",
            XpReward = 50,
            Current = Math.Min(standardsFixed * 2, 10),
            Target = 10
        });

        // Quest: complete advanced tutorials
        int advancedCompleted = achievements.Count(a => a.IsUnlocked && a.XpReward >= 100);
        ActiveQuests.Add(new QuestItem
        {
            Title = "Aprendiz Veloz",
            Description = "Conclua 1 tutorial avançado 'Regex Ninja'.",
            XpReward = 100,
            Current = Math.Min(advancedCompleted, 1),
            Target = 1
        });
    }

    private void LoadLeaderboard()
    {
        Leaderboard.Clear();

        // Seed with sample entries — in production this would come from the API
        Leaderboard.Add(new LeaderboardEntry { Rank = 1, Name = "Sarah Jenkins", Title = "LOD Master", XpPoints = 2480 });
        Leaderboard.Add(new LeaderboardEntry { Rank = 2, Name = "Michael Chen", Title = "Regex Guru", XpPoints = 2150 });
        Leaderboard.Add(new LeaderboardEntry { Rank = 3, Name = "Emma Watts", Title = "Wall Wizard", XpPoints = 1920 });

        // Current user
        Leaderboard.Add(new LeaderboardEntry
        {
            Rank = 12,
            Name = UserName,
            Title = LevelTitle,
            XpPoints = CurrentXp,
            IsCurrentUser = true
        });
    }

    private void LoadRewards()
    {
        Rewards.Clear();
        Rewards.Add(new RewardItem { Title = "Tema Midnight", Description = "Desbloqueado no Nível 10", Icon = "palette", RequiredLevel = 10, IsUnlocked = UserLevel >= 10 });
        Rewards.Add(new RewardItem { Title = "Certificação Oficial", Description = "Alcance Nível 20 para desbloquear.", Icon = "workspace_premium", RequiredLevel = 20, IsUnlocked = UserLevel >= 20 });
        Rewards.Add(new RewardItem { Title = "Scripts API Personalizados", Description = "Alcance Nível 25 para desbloquear.", Icon = "api", RequiredLevel = 25, IsUnlocked = UserLevel >= 25 });
        Rewards.Add(new RewardItem { Title = "AI Helper Pro", Description = "Alcance Nível 30 para desbloquear.", Icon = "auto_awesome", RequiredLevel = 30, IsUnlocked = UserLevel >= 30 });
    }

    [RelayCommand]
    private void SetFilter(string mode)
    {
        FilterMode = mode;
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        FilteredAchievements.Clear();
        var source = FilterMode switch
        {
            "Unlocked" => AllAchievements.Where(a => a.IsUnlocked),
            "Locked"   => AllAchievements.Where(a => !a.IsUnlocked),
            _          => AllAchievements.AsEnumerable()
        };
        foreach (var a in source) FilteredAchievements.Add(a);
    }

    [RelayCommand]
    private void OpenWindow(string windowName) => _navigation.NavigateTo(windowName);

    private void CalculateLevelProgress()
    {
        const int xpPerLevel = 1000;
        XpForNextLevel       = (UserLevel + 1) * xpPerLevel;
        int xpInCurrentLevel = CurrentXp % xpPerLevel;
        LevelProgressPercent = (double)xpInCurrentLevel / xpPerLevel * 100;
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

/// <summary>Represents an active quest/challenge.</summary>
public class QuestItem
{
    public string Title       { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int    XpReward    { get; set; }
    public int    Current     { get; set; }
    public int    Target      { get; set; }
    public double ProgressPercent => Target == 0 ? 0 : (double)Current / Target * 100;
}

/// <summary>Represents a leaderboard entry.</summary>
public class LeaderboardEntry
{
    public int    Rank     { get; set; }
    public string Name     { get; set; } = string.Empty;
    public string Title    { get; set; } = string.Empty;
    public int    XpPoints { get; set; }
    public bool   IsCurrentUser { get; set; }
}

/// <summary>Represents an unlockable reward.</summary>
public class RewardItem
{
    public string Title       { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon        { get; set; } = string.Empty;
    public int    RequiredLevel { get; set; }
    public bool   IsUnlocked  { get; set; }
}
