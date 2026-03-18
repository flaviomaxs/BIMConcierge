using BIMConcierge.Core.Interfaces;
using BIMConcierge.Core.Models;
using Serilog;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace BIMConcierge.Infrastructure.Revit;

public interface IRevitEventDispatcher : IDisposable
{
    event EventHandler<CorrectionEvent> CorrectionRaised;

    /// <summary>
    /// Attach is called by the Plugin layer (the only project that references RevitAPI.dll).
    /// Parameter typed as object to avoid an Autodesk reference in Infrastructure.
    /// </summary>
    void Attach(object uiApplicationObject);
    void Detach();
}

/// <summary>
/// Coordinates correction events between Plugin (Revit API) and Infrastructure (standards logic).
/// Implements a rule engine that validates elements against company standards in real time,
/// deduplicates corrections, tracks dismissals, and supports auto-fix via ExternalEvent.
/// Real Revit API wiring lives in Plugin/RevitEventBridge.cs.
/// </summary>
public sealed class RevitEventDispatcher : IRevitEventDispatcher
{
    private readonly ILocalDatabase _db;

    public event EventHandler<CorrectionEvent>? CorrectionRaised;

    private Action? _detachAction;

    // Standards loaded from the cache, keyed by companyId for quick lookup
    private List<CompanyStandard> _standards = [];
    private bool _standardsLoaded;

    // Track raised corrections so AutoFix can look them up
    private readonly ConcurrentDictionary<string, CorrectionEvent> _activeCorrections = new();

    // Deduplication: key = "elementId|ruleId" → correction Id (prevents duplicate corrections)
    private readonly ConcurrentDictionary<string, string> _deduplicationMap = new();

    // Dismissed corrections: key = "elementId|ruleId" (user chose to ignore)
    private readonly ConcurrentDictionary<string, bool> _dismissed = new();

    // Callback that Plugin registers to execute auto-fix commands in Revit context (async for ExternalEvent)
    private Func<string, string?, Task<bool>>? _autoFixHandler;

    // When true, corrections with CanAutoFix are automatically fixed on detection
    private volatile bool _autoFixOnDetection;

    // Compiled regex cache to avoid recompiling on every DocumentChanged event
    private readonly ConcurrentDictionary<string, Regex?> _regexCache = new();

    public RevitEventDispatcher(ILocalDatabase db)
    {
        _db = db;
    }

    // ── Statistics ───────────────────────────────────────────────────────────

    public int ActiveCount => _activeCorrections.Values.Count(c => !c.IsFixed);
    public int FixedCount => _activeCorrections.Values.Count(c => c.IsFixed);
    public int TotalValidated => _activeCorrections.Count;
    public bool StandardsLoaded => _standardsLoaded;
    public int StandardsCount => _standards.Count;

    // ── Auto-fix on detection toggle ────────────────────────────────────────

    public bool AutoFixOnDetection
    {
        get => _autoFixOnDetection;
        set => _autoFixOnDetection = value;
    }

    // ── Lifecycle ───────────────────────────────────────────────────────────

    public void Attach(object uiApplicationObject)
    {
        // Intentionally empty here; Plugin/RevitEventBridge calls RegisterDetach
        // and routes DocumentChanged events back via RaiseCorrection / ValidateElements.
    }

    public void Detach() => _detachAction?.Invoke();

    /// <summary>Plugin layer registers its own cleanup logic here.</summary>
    public void RegisterDetach(Action detach) => _detachAction = detach;

    /// <summary>Plugin registers a handler that can execute Revit commands for auto-fix via ExternalEvent.</summary>
    public void RegisterAutoFixHandler(Func<string, string?, Task<bool>> handler) => _autoFixHandler = handler;

    // ── Standards loading ───────────────────────────────────────────────────

    /// <summary>
    /// Loads standards from local cache and updates the in-memory rule set.
    /// Called by the Plugin after login or when standards change.
    /// </summary>
    public async Task LoadStandardsAsync(string companyId)
    {
        _standards = await _db.GetStandardsAsync(companyId);
        _regexCache.Clear();

        // Pre-compile regex patterns for performance
        foreach (var standard in _standards.Where(s => s.IsActive && !string.IsNullOrEmpty(s.Rule)))
        {
            _regexCache.TryAdd(standard.Rule, CompileRegex(standard.Rule));
        }

        _standardsLoaded = true;
        Log.Information("Loaded {Count} company standards for validation ({Active} active)",
            _standards.Count, _standards.Count(s => s.IsActive));
    }

    /// <summary>
    /// Reloads standards from local cache. Call after standards are updated via API.
    /// </summary>
    public async Task ReloadStandardsAsync(string companyId)
    {
        _standardsLoaded = false;
        await LoadStandardsAsync(companyId);

        // Re-validate active corrections against new rules — remove stale ones
        var staleKeys = new List<string>();
        foreach (var kvp in _activeCorrections)
        {
            if (kvp.Value.IsFixed) continue;
            var standard = _standards.FirstOrDefault(s => s.Id == kvp.Value.RuleId);
            if (standard is null || !standard.IsActive)
                staleKeys.Add(kvp.Key);
        }

        foreach (var key in staleKeys)
        {
            _activeCorrections.TryRemove(key, out _);
            // Also clean dedup map
            var dedupeEntries = _deduplicationMap.Where(d => d.Value == key).Select(d => d.Key).ToList();
            foreach (var de in dedupeEntries)
                _deduplicationMap.TryRemove(de, out _);
        }

        if (staleKeys.Count > 0)
            Log.Information("Removed {Count} stale corrections after standards reload", staleKeys.Count);
    }

    // ── Correction management ───────────────────────────────────────────────

    /// <summary>Called by RevitEventBridge in the Plugin project.</summary>
    public void RaiseCorrection(CorrectionEvent ev)
    {
        _activeCorrections[ev.Id] = ev;
        CorrectionRaised?.Invoke(this, ev);
    }

    /// <summary>
    /// Dismisses a correction so it won't be raised again for the same element+rule.
    /// </summary>
    public void DismissCorrection(string correctionEventId)
    {
        if (_activeCorrections.TryRemove(correctionEventId, out var ev))
        {
            string dedupeKey = BuildDedupeKey(ev.ElementId ?? "", ev.RuleId);
            _dismissed.TryAdd(dedupeKey, true);

            // Clean dedup map
            _deduplicationMap.TryRemove(dedupeKey, out _);

            Log.Information("Correction {Id} dismissed (element={ElementId}, rule={RuleId})",
                correctionEventId, ev.ElementId, ev.RuleId);
        }
    }

    /// <summary>
    /// Clears all dismissed corrections, allowing them to be raised again.
    /// </summary>
    public void ClearDismissals()
    {
        _dismissed.Clear();
        Log.Information("All correction dismissals cleared");
    }

    // ── Validation engine ───────────────────────────────────────────────────

    /// <summary>
    /// Validates element names/properties provided by the Plugin layer against loaded standards.
    /// Deduplicates: same element + same rule won't create a second correction.
    /// Skips dismissed corrections.
    /// Optionally auto-fixes on detection if enabled.
    /// </summary>
    public List<CorrectionEvent> ValidateElements(List<(string ElementId, string Name, string Category)> elements)
    {
        if (!_standardsLoaded || _standards.Count == 0)
            return [];

        var corrections = new List<CorrectionEvent>();

        foreach (CompanyStandard standard in _standards.Where(s => s.IsActive))
        {
            if (string.IsNullOrEmpty(standard.Rule))
                continue;

            Regex? regex = GetCachedRegex(standard.Rule);
            if (regex is null)
                continue;

            foreach ((string elementId, string name, string category) in elements)
            {
                if (!MatchesCategory(standard.Category, category))
                    continue;

                string dedupeKey = BuildDedupeKey(elementId, standard.Id);

                // Skip if dismissed by user
                if (_dismissed.ContainsKey(dedupeKey))
                    continue;

                try
                {
                    if (!regex.IsMatch(name))
                    {
                        // Check for existing unfixed correction (dedup)
                        if (_deduplicationMap.TryGetValue(dedupeKey, out string? existingId)
                            && _activeCorrections.TryGetValue(existingId, out var existing)
                            && !existing.IsFixed)
                        {
                            // Already tracked — skip
                            continue;
                        }

                        var ev = new CorrectionEvent
                        {
                            RuleId = standard.Id,
                            Title = standard.Name,
                            Description = $"Elemento '{name}' não atende ao padrão: {standard.Description}",
                            Severity = standard.AlertLevel,
                            CanAutoFix = standard.AutoFix,
                            ElementId = elementId
                        };

                        _deduplicationMap[dedupeKey] = ev.Id;
                        corrections.Add(ev);
                        RaiseCorrection(ev);

                        // Auto-fix on detection if enabled
                        if (_autoFixOnDetection && ev.CanAutoFix && _autoFixHandler is not null)
                        {
                            _ = TryAutoFixAsync(ev.Id); // Fire-and-forget
                        }
                    }
                    else
                    {
                        // Element now passes the rule — clear any previous correction
                        if (_deduplicationMap.TryRemove(dedupeKey, out string? clearedId))
                        {
                            if (_activeCorrections.TryGetValue(clearedId, out var cleared) && !cleared.IsFixed)
                            {
                                cleared.IsFixed = true;
                                Log.Debug("Correction {Id} auto-resolved: element {ElementId} now passes rule {RuleId}",
                                    clearedId, elementId, standard.Id);
                            }
                        }
                    }
                }
                catch (RegexMatchTimeoutException)
                {
                    Log.Warning("Regex timeout evaluating standard {StandardId} rule '{Rule}' on element {ElementId}",
                        standard.Id, standard.Rule, elementId);
                }
            }
        }

        return corrections;
    }

    /// <summary>
    /// Returns all active (unfixed, non-dismissed) corrections.
    /// Called by StandardsService.ValidateModelAsync().
    /// </summary>
    public List<CorrectionEvent> RunValidation() =>
        _activeCorrections.Values
            .Where(c => !c.IsFixed)
            .OrderByDescending(c => c.Severity)
            .ThenByDescending(c => c.OccurredAt)
            .ToList();

    // ── Auto-fix ────────────────────────────────────────────────────────────

    /// <summary>
    /// Attempts to auto-fix a correction by delegating to the Plugin's ExternalEvent handler.
    /// </summary>
    public async Task<bool> TryAutoFixAsync(string correctionEventId)
    {
        if (!_activeCorrections.TryGetValue(correctionEventId, out CorrectionEvent? ev))
        {
            Log.Warning("Auto-fix requested for unknown correction {Id}", correctionEventId);
            return false;
        }

        if (!ev.CanAutoFix)
        {
            Log.Warning("Correction {Id} does not support auto-fix", correctionEventId);
            return false;
        }

        CompanyStandard? standard = _standards.FirstOrDefault(s => s.Id == ev.RuleId);

        if (_autoFixHandler is not null)
        {
            bool success = await _autoFixHandler(ev.ElementId ?? string.Empty, standard?.Rule);
            if (success)
            {
                ev.IsFixed = true;
                Log.Information("Auto-fix applied for correction {Id} on element {ElementId}", correctionEventId, ev.ElementId);
            }
            return success;
        }

        Log.Warning("No auto-fix handler registered — cannot fix correction {Id}", correctionEventId);
        return false;
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static string BuildDedupeKey(string elementId, string ruleId) => $"{elementId}|{ruleId}";

    private Regex? GetCachedRegex(string pattern) =>
        _regexCache.GetOrAdd(pattern, p => CompileRegex(p));

    private static Regex? CompileRegex(string pattern)
    {
        try
        {
            return new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100));
        }
        catch (ArgumentException ex)
        {
            Log.Error(ex, "Invalid regex pattern: '{Pattern}'", pattern);
            return null;
        }
    }

    private static bool MatchesCategory(string standardCategory, string elementCategory)
    {
        if (string.IsNullOrEmpty(standardCategory)) return true;
        return elementCategory.Contains(standardCategory, StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        Detach();
        _activeCorrections.Clear();
        _deduplicationMap.Clear();
        _dismissed.Clear();
        _regexCache.Clear();
        GC.SuppressFinalize(this);
    }
}
