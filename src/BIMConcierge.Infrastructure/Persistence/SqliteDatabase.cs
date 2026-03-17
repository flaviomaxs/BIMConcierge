using BIMConcierge.Core.Interfaces;
using BIMConcierge.Core.Models;
using Newtonsoft.Json;
using Serilog;
using SQLite;

namespace BIMConcierge.Infrastructure.Persistence;

/// <summary>
/// SQLite-backed local cache for offline use.
/// </summary>
public sealed class SqliteDatabase : ILocalDatabase
{
    private readonly SQLiteAsyncConnection _conn;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _initialized;

    public SqliteDatabase()
    {
        var dbDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "BIMConcierge");
        Directory.CreateDirectory(dbDir);

        var dbPath = Path.Combine(dbDir, "cache.db");
        _conn = new SQLiteAsyncConnection(dbPath);
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

            if (row is null) return null;

            return new User
            {
                Id = row.Id,
                Name = row.Name,
                Email = row.Email,
                Role = row.Role,
                CompanyId = row.CompanyId,
                AvatarUrl = row.AvatarUrl,
                XpPoints = row.XpPoints,
                Level = row.Level,
                CreatedAt = row.CreatedAt
            };
        }
        catch (Exception ex) { Log.Error(ex, "Failed to load cached user"); return null; }
    }

    // ── Tutorials ──────────────────────────────────────────────────────────────

    public async Task SaveTutorialsAsync(List<Tutorial> tutorials)
    {
        await EnsureInitializedAsync();
        try
        {
            var rows = tutorials.Select(t => new TutorialRow
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
            }).ToList();

            await _conn.RunInTransactionAsync(conn =>
            {
                foreach (var row in rows)
                    conn.InsertOrReplace(row);
            });
        }
        catch (Exception ex) { Log.Error(ex, "Failed to save tutorials to local cache"); }
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

    // ── Progress ───────────────────────────────────────────────────────────────

    public async Task SaveProgressAsync(TutorialProgress progress)
    {
        await EnsureInitializedAsync();
        try
        {
            var row = new ProgressRow
            {
                Key = $"{progress.UserId}_{progress.TutorialId}",
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
            var key = $"{userId}_{tutorialId}";
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

    // ── Standards ──────────────────────────────────────────────────────────────

    public async Task SaveStandardsAsync(List<CompanyStandard> standards)
    {
        await EnsureInitializedAsync();
        try
        {
            var rows = standards.Select(s => new StandardRow
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
            }).ToList();

            await _conn.RunInTransactionAsync(conn =>
            {
                foreach (var row in rows)
                    conn.InsertOrReplace(row);
            });
        }
        catch (Exception ex) { Log.Error(ex, "Failed to save standards to local cache"); }
    }

    public async Task<List<CompanyStandard>> GetStandardsAsync(string companyId)
    {
        await EnsureInitializedAsync();
        try
        {
            var rows = await _conn.Table<StandardRow>()
                .Where(s => s.CompanyId == companyId)
                .ToListAsync();

            return rows.Select(r => new CompanyStandard
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
            }).ToList();
        }
        catch (Exception ex) { Log.Error(ex, "Failed to load cached standards"); return []; }
    }

    // ── License ──────────────────────────────────────────────────────────────

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

    // ── Mapping helpers ────────────────────────────────────────────────────────

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
}
