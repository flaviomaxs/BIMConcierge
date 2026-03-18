using BIMConcierge.Core.Interfaces;
using BIMConcierge.Core.Models;
using Newtonsoft.Json;
using Serilog;
using SQLite;

namespace BIMConcierge.Infrastructure.Persistence;

/// <summary>
/// SQLite-backed local cache for offline use.
/// Implements full CRUD for all domain entities.
/// </summary>
public sealed class SqliteDatabase : ILocalDatabase
{
    private readonly SQLiteAsyncConnection _conn;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private readonly string _dbPath;
    private bool _initialized;

    public SqliteDatabase()
    {
        var dbDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "BIMConcierge");
        Directory.CreateDirectory(dbDir);

        _dbPath = Path.Combine(dbDir, "cache.db");
        _conn = new SQLiteAsyncConnection(_dbPath);
    }

    private async Task EnsureInitializedAsync()
    {
        if (_initialized) return;
        await _initLock.WaitAsync();
        try
        {
            if (_initialized) return;
            await _conn.CreateTableAsync<UserRow>();
            await _conn.CreateTableAsync<TutorialRow>();
            await _conn.CreateTableAsync<ProgressRow>();
            await _conn.CreateTableAsync<StandardRow>();
            await _conn.CreateTableAsync<AchievementRow>();
            await _conn.CreateTableAsync<LicenseRow>();
            _initialized = true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to initialize local database");
        }
        finally
        {
            _initLock.Release();
        }
    }

    // ── User ───────────────────────────────────────────────────────────────────

    public async Task SaveUserAsync(User user)
    {
        await EnsureInitializedAsync();
        try
        {
            var row = new UserRow
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role,
                CompanyId = user.CompanyId,
                AvatarUrl = user.AvatarUrl,
                XpPoints = user.XpPoints,
                Level = user.Level,
                CreatedAt = user.CreatedAt,
                CachedAt = DateTime.UtcNow
            };
            await _conn.InsertOrReplaceAsync(row);
        }
        catch (Exception ex) { Log.Error(ex, "Failed to save user to local cache"); }
    }

    public async Task<User?> GetLastUserAsync()
    {
        await EnsureInitializedAsync();
        try
        {
            var row = await _conn.Table<UserRow>()
                .OrderByDescending(u => u.CachedAt)
                .FirstOrDefaultAsync();
            return row is null ? null : ToUser(row);
        }
        catch (Exception ex) { Log.Error(ex, "Failed to load cached user"); return null; }
    }

    public async Task<User?> GetUserAsync(string id)
    {
        await EnsureInitializedAsync();
        try
        {
            var row = await _conn.FindAsync<UserRow>(id);
            return row is null ? null : ToUser(row);
        }
        catch (Exception ex) { Log.Error(ex, "Failed to load user {Id}", id); return null; }
    }

    public async Task DeleteUserAsync(string id)
    {
        await EnsureInitializedAsync();
        try { await _conn.DeleteAsync<UserRow>(id); }
        catch (Exception ex) { Log.Error(ex, "Failed to delete user {Id}", id); }
    }

    // ── Tutorials ──────────────────────────────────────────────────────────────

    public async Task SaveTutorialsAsync(List<Tutorial> tutorials)
    {
        await EnsureInitializedAsync();
        try
        {
            var rows = tutorials.Select(ToTutorialRow).ToList();
            await _conn.RunInTransactionAsync(conn =>
            {
                foreach (var row in rows)
                    conn.InsertOrReplace(row);
            });
        }
        catch (Exception ex) { Log.Error(ex, "Failed to save tutorials to local cache"); }
    }

    public async Task SaveTutorialAsync(Tutorial tutorial)
    {
        await EnsureInitializedAsync();
        try { await _conn.InsertOrReplaceAsync(ToTutorialRow(tutorial)); }
        catch (Exception ex) { Log.Error(ex, "Failed to save tutorial {Id}", tutorial.Id); }
    }

    public async Task<List<Tutorial>> GetTutorialsAsync(string? category = null)
    {
        await EnsureInitializedAsync();
        try
        {
            var query = _conn.Table<TutorialRow>();
            if (!string.IsNullOrEmpty(category))
                query = query.Where(t => t.Category == category);

            var rows = await query.ToListAsync();
            return rows.Select(ToTutorial).ToList();
        }
        catch (Exception ex) { Log.Error(ex, "Failed to load cached tutorials"); return []; }
    }

    public async Task<Tutorial?> GetTutorialAsync(string id)
    {
        await EnsureInitializedAsync();
        try
        {
            var row = await _conn.FindAsync<TutorialRow>(id);
            return row is null ? null : ToTutorial(row);
        }
        catch (Exception ex) { Log.Error(ex, "Failed to load cached tutorial {Id}", id); return null; }
    }

    public async Task DeleteTutorialAsync(string id)
    {
        await EnsureInitializedAsync();
        try { await _conn.DeleteAsync<TutorialRow>(id); }
        catch (Exception ex) { Log.Error(ex, "Failed to delete tutorial {Id}", id); }
    }

    // ── Progress ───────────────────────────────────────────────────────────────

    public async Task SaveProgressAsync(TutorialProgress progress)
    {
        await EnsureInitializedAsync();
        try
        {
            var row = new ProgressRow
            {
                Key = BuildProgressKey(progress.UserId, progress.TutorialId),
                UserId = progress.UserId,
                TutorialId = progress.TutorialId,
                CurrentStep = progress.CurrentStep,
                TotalSteps = progress.TotalSteps,
                IsCompleted = progress.IsCompleted,
                ScorePercent = progress.ScorePercent,
                StartedAt = progress.StartedAt,
                CompletedAt = progress.CompletedAt
            };
            await _conn.InsertOrReplaceAsync(row);
        }
        catch (Exception ex) { Log.Error(ex, "Failed to save progress to local cache"); }
    }

    public async Task<TutorialProgress?> GetProgressAsync(string userId, string tutorialId)
    {
        await EnsureInitializedAsync();
        try
        {
            var key = BuildProgressKey(userId, tutorialId);
            var row = await _conn.FindAsync<ProgressRow>(key);
            return row is null ? null : ToProgress(row);
        }
        catch (Exception ex) { Log.Error(ex, "Failed to load cached progress"); return null; }
    }

    public async Task<List<TutorialProgress>> GetAllProgressAsync(string userId)
    {
        await EnsureInitializedAsync();
        try
        {
            var rows = await _conn.Table<ProgressRow>()
                .Where(p => p.UserId == userId)
                .ToListAsync();
            return rows.Select(ToProgress).ToList();
        }
        catch (Exception ex) { Log.Error(ex, "Failed to load cached progress list"); return []; }
    }

    public async Task DeleteProgressAsync(string userId, string tutorialId)
    {
        await EnsureInitializedAsync();
        try
        {
            var key = BuildProgressKey(userId, tutorialId);
            await _conn.DeleteAsync<ProgressRow>(key);
        }
        catch (Exception ex) { Log.Error(ex, "Failed to delete progress for user {UserId} tutorial {TutorialId}", userId, tutorialId); }
    }

    // ── Standards ──────────────────────────────────────────────────────────────

    public async Task SaveStandardsAsync(List<CompanyStandard> standards)
    {
        await EnsureInitializedAsync();
        try
        {
            var rows = standards.Select(ToStandardRow).ToList();
            await _conn.RunInTransactionAsync(conn =>
            {
                foreach (var row in rows)
                    conn.InsertOrReplace(row);
            });
        }
        catch (Exception ex) { Log.Error(ex, "Failed to save standards to local cache"); }
    }

    public async Task SaveStandardAsync(CompanyStandard standard)
    {
        await EnsureInitializedAsync();
        try { await _conn.InsertOrReplaceAsync(ToStandardRow(standard)); }
        catch (Exception ex) { Log.Error(ex, "Failed to save standard {Id}", standard.Id); }
    }

    public async Task<List<CompanyStandard>> GetStandardsAsync(string companyId)
    {
        await EnsureInitializedAsync();
        try
        {
            var rows = await _conn.Table<StandardRow>()
                .Where(s => s.CompanyId == companyId)
                .ToListAsync();
            return rows.Select(ToStandard).ToList();
        }
        catch (Exception ex) { Log.Error(ex, "Failed to load cached standards"); return []; }
    }

    public async Task<CompanyStandard?> GetStandardByIdAsync(string id)
    {
        await EnsureInitializedAsync();
        try
        {
            var row = await _conn.FindAsync<StandardRow>(id);
            return row is null ? null : ToStandard(row);
        }
        catch (Exception ex) { Log.Error(ex, "Failed to load standard {Id}", id); return null; }
    }

    public async Task DeleteStandardAsync(string id)
    {
        await EnsureInitializedAsync();
        try { await _conn.DeleteAsync<StandardRow>(id); }
        catch (Exception ex) { Log.Error(ex, "Failed to delete standard {Id}", id); }
    }

    // ── Achievements ──────────────────────────────────────────────────────────

    public async Task SaveAchievementsAsync(List<Achievement> achievements)
    {
        await EnsureInitializedAsync();
        try
        {
            var rows = achievements.Select(a => ToAchievementRow(a, string.Empty)).ToList();
            await _conn.RunInTransactionAsync(conn =>
            {
                foreach (var row in rows)
                    conn.InsertOrReplace(row);
            });
        }
        catch (Exception ex) { Log.Error(ex, "Failed to save achievements to local cache"); }
    }

    public async Task<List<Achievement>> GetAchievementsAsync(string userId)
    {
        await EnsureInitializedAsync();
        try
        {
            var rows = await _conn.Table<AchievementRow>()
                .Where(a => a.UserId == userId)
                .ToListAsync();
            return rows.Select(ToAchievement).ToList();
        }
        catch (Exception ex) { Log.Error(ex, "Failed to load cached achievements"); return []; }
    }

    public async Task SaveAchievementAsync(Achievement achievement, string userId)
    {
        await EnsureInitializedAsync();
        try { await _conn.InsertOrReplaceAsync(ToAchievementRow(achievement, userId)); }
        catch (Exception ex) { Log.Error(ex, "Failed to save achievement {Id}", achievement.Id); }
    }

    // ── License ────────────────────────────────────────────────────────────────

    public async Task SaveLicenseAsync(License license)
    {
        await EnsureInitializedAsync();
        try
        {
            var row = new LicenseRow
            {
                Key = license.Key,
                CompanyId = license.CompanyId,
                MaxSeats = license.MaxSeats,
                UsedSeats = license.UsedSeats,
                Type = (int)license.Type,
                ExpiresAt = license.ExpiresAt,
                CachedAt = DateTime.UtcNow
            };
            await _conn.InsertOrReplaceAsync(row);
        }
        catch (Exception ex) { Log.Error(ex, "Failed to save license to local cache"); }
    }

    public async Task<License?> GetCachedLicenseAsync(string companyId)
    {
        await EnsureInitializedAsync();
        try
        {
            var row = await _conn.Table<LicenseRow>()
                .Where(l => l.CompanyId == companyId)
                .OrderByDescending(l => l.CachedAt)
                .FirstOrDefaultAsync();

            if (row is null) return null;

            return new License
            {
                Key = row.Key,
                CompanyId = row.CompanyId,
                MaxSeats = row.MaxSeats,
                UsedSeats = row.UsedSeats,
                Type = (LicenseType)row.Type,
                ExpiresAt = row.ExpiresAt
            };
        }
        catch (Exception ex) { Log.Error(ex, "Failed to load cached license"); return null; }
    }

    public async Task DeleteLicenseAsync(string companyId)
    {
        await EnsureInitializedAsync();
        try
        {
            var rows = await _conn.Table<LicenseRow>()
                .Where(l => l.CompanyId == companyId)
                .ToListAsync();
            foreach (var row in rows)
                await _conn.DeleteAsync(row);
        }
        catch (Exception ex) { Log.Error(ex, "Failed to delete license for company {CompanyId}", companyId); }
    }

    // ── Maintenance ───────────────────────────────────────────────────────────

    public async Task ClearAllAsync()
    {
        await EnsureInitializedAsync();
        try
        {
            await _conn.DeleteAllAsync<UserRow>();
            await _conn.DeleteAllAsync<TutorialRow>();
            await _conn.DeleteAllAsync<ProgressRow>();
            await _conn.DeleteAllAsync<StandardRow>();
            await _conn.DeleteAllAsync<AchievementRow>();
            await _conn.DeleteAllAsync<LicenseRow>();
            Log.Information("Local database cleared");
        }
        catch (Exception ex) { Log.Error(ex, "Failed to clear local database"); }
    }

    public Task<long> GetDatabaseSizeAsync()
    {
        try
        {
            var info = new FileInfo(_dbPath);
            return Task.FromResult(info.Exists ? info.Length : 0L);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to get database size");
            return Task.FromResult(0L);
        }
    }

    // ── Dispose ─────────────────────────────────────────────────────────────

    public void Dispose()
    {
        _conn.GetConnection().Close();
        _initLock.Dispose();
    }

    // ── Mapping: Domain → Row ───────────────────────────────────────────────

    private static string BuildProgressKey(string userId, string tutorialId) => $"{userId}_{tutorialId}";

    private static TutorialRow ToTutorialRow(Tutorial t) => new()
    {
        Id = t.Id,
        Title = t.Title,
        Description = t.Description,
        Category = t.Category,
        Difficulty = t.Difficulty,
        DurationMins = t.DurationMins,
        StepCount = t.StepCount,
        ThumbnailUrl = t.ThumbnailUrl,
        IsCompanyOwned = t.IsCompanyOwned,
        StepsJson = JsonConvert.SerializeObject(t.Steps)
    };

    private static StandardRow ToStandardRow(CompanyStandard s) => new()
    {
        Id = s.Id,
        CompanyId = s.CompanyId,
        Category = s.Category,
        Name = s.Name,
        Description = s.Description,
        Rule = s.Rule,
        IsActive = s.IsActive,
        AutoFix = s.AutoFix,
        AlertLevel = (int)s.AlertLevel
    };

    private static AchievementRow ToAchievementRow(Achievement a, string userId) => new()
    {
        Key = $"{userId}_{a.Id}",
        UserId = userId,
        AchievementId = a.Id,
        Title = a.Title,
        Description = a.Description,
        Icon = a.Icon,
        XpReward = a.XpReward,
        IsUnlocked = a.IsUnlocked,
        UnlockedAt = a.UnlockedAt
    };

    // ── Mapping: Row → Domain ───────────────────────────────────────────────

    private static User ToUser(UserRow r) => new()
    {
        Id = r.Id,
        Name = r.Name,
        Email = r.Email,
        Role = r.Role,
        CompanyId = r.CompanyId,
        AvatarUrl = r.AvatarUrl,
        XpPoints = r.XpPoints,
        Level = r.Level,
        CreatedAt = r.CreatedAt
    };

    private static Tutorial ToTutorial(TutorialRow r) => new()
    {
        Id = r.Id,
        Title = r.Title,
        Description = r.Description,
        Category = r.Category,
        Difficulty = r.Difficulty,
        DurationMins = r.DurationMins,
        StepCount = r.StepCount,
        ThumbnailUrl = r.ThumbnailUrl,
        IsCompanyOwned = r.IsCompanyOwned,
        Steps = string.IsNullOrEmpty(r.StepsJson)
            ? []
            : JsonConvert.DeserializeObject<List<TutorialStep>>(r.StepsJson) ?? []
    };

    private static TutorialProgress ToProgress(ProgressRow r) => new()
    {
        UserId = r.UserId,
        TutorialId = r.TutorialId,
        CurrentStep = r.CurrentStep,
        TotalSteps = r.TotalSteps,
        IsCompleted = r.IsCompleted,
        ScorePercent = r.ScorePercent,
        StartedAt = r.StartedAt,
        CompletedAt = r.CompletedAt
    };

    private static CompanyStandard ToStandard(StandardRow r) => new()
    {
        Id = r.Id,
        CompanyId = r.CompanyId,
        Category = r.Category,
        Name = r.Name,
        Description = r.Description,
        Rule = r.Rule,
        IsActive = r.IsActive,
        AutoFix = r.AutoFix,
        AlertLevel = (Severity)r.AlertLevel
    };

    private static Achievement ToAchievement(AchievementRow r) => new()
    {
        Id = r.AchievementId,
        Title = r.Title,
        Description = r.Description,
        Icon = r.Icon,
        XpReward = r.XpReward,
        IsUnlocked = r.IsUnlocked,
        UnlockedAt = r.UnlockedAt
    };

    // ── SQLite row types ───────────────────────────────────────────────────────

    [Table("Users")]
    private sealed class UserRow
    {
        [PrimaryKey] public string Id        { get; set; } = string.Empty;
        public string Name                   { get; set; } = string.Empty;
        public string Email                  { get; set; } = string.Empty;
        public string Role                   { get; set; } = string.Empty;
        public string CompanyId              { get; set; } = string.Empty;
        public string AvatarUrl              { get; set; } = string.Empty;
        public int    XpPoints               { get; set; }
        public int    Level                  { get; set; }
        public DateTime CreatedAt            { get; set; }
        public DateTime CachedAt             { get; set; }
    }

    [Table("Tutorials")]
    private sealed class TutorialRow
    {
        [PrimaryKey] public string Id        { get; set; } = string.Empty;
        public string Title                  { get; set; } = string.Empty;
        public string Description            { get; set; } = string.Empty;
        public string Category               { get; set; } = string.Empty;
        public string Difficulty             { get; set; } = string.Empty;
        public int    DurationMins           { get; set; }
        public int    StepCount              { get; set; }
        public string ThumbnailUrl           { get; set; } = string.Empty;
        public bool   IsCompanyOwned         { get; set; }
        public string StepsJson              { get; set; } = string.Empty;
    }

    [Table("Progress")]
    private sealed class ProgressRow
    {
        [PrimaryKey] public string Key       { get; set; } = string.Empty;
        [Indexed]    public string UserId    { get; set; } = string.Empty;
        public string TutorialId             { get; set; } = string.Empty;
        public int    CurrentStep            { get; set; }
        public int    TotalSteps             { get; set; }
        public bool   IsCompleted            { get; set; }
        public int    ScorePercent           { get; set; }
        public DateTime  StartedAt           { get; set; }
        public DateTime? CompletedAt         { get; set; }
    }

    [Table("Standards")]
    private sealed class StandardRow
    {
        [PrimaryKey] public string Id        { get; set; } = string.Empty;
        [Indexed]    public string CompanyId { get; set; } = string.Empty;
        public string Category               { get; set; } = string.Empty;
        public string Name                   { get; set; } = string.Empty;
        public string Description            { get; set; } = string.Empty;
        public string Rule                   { get; set; } = string.Empty;
        public bool   IsActive               { get; set; }
        public bool   AutoFix                { get; set; }
        public int    AlertLevel             { get; set; }
    }

    [Table("Achievements")]
    private sealed class AchievementRow
    {
        [PrimaryKey] public string Key          { get; set; } = string.Empty;  // "userId_achievementId"
        [Indexed]    public string UserId        { get; set; } = string.Empty;
        public string AchievementId              { get; set; } = string.Empty;
        public string Title                      { get; set; } = string.Empty;
        public string Description                { get; set; } = string.Empty;
        public string Icon                       { get; set; } = string.Empty;
        public int    XpReward                   { get; set; }
        public bool   IsUnlocked                 { get; set; }
        public DateTime? UnlockedAt              { get; set; }
    }

    [Table("Licenses")]
    private sealed class LicenseRow
    {
        [PrimaryKey] public string Key       { get; set; } = string.Empty;
        [Indexed]    public string CompanyId  { get; set; } = string.Empty;
        public int    MaxSeats               { get; set; }
        public int    UsedSeats              { get; set; }
        public int    Type                   { get; set; }
        public DateTime ExpiresAt            { get; set; }
        public DateTime CachedAt             { get; set; }
    }
}
