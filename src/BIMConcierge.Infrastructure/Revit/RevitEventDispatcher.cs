using BIMConcierge.Core.Interfaces;
using BIMConcierge.Core.Models;
using Serilog;
using System.Collections.Concurrent;

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
/// Real Revit API wiring lives in Plugin/RevitEventBridge.cs.
/// </summary>
public sealed class RevitEventDispatcher : IRevitEventDispatcher
{
    private readonly ILocalDatabase _db;

    public event EventHandler<CorrectionEvent>? CorrectionRaised;

    private Action? _detachAction;

    // Standards loaded from the cache, keyed by companyId for quick lookup
    private List<CompanyStandard> _standards = [];

    // Track raised corrections so AutoFix can look them up
    private readonly ConcurrentDictionary<string, CorrectionEvent> _activeCorrections = new();

    // Callback that Plugin registers to execute auto-fix commands in Revit context
    private Func<string, string?, bool>? _autoFixHandler;

    public RevitEventDispatcher(ILocalDatabase db)
    {
        _db = db;
    }

    public void Attach(object uiApplicationObject)
    {
        // Intentionally empty here; Plugin/RevitEventBridge calls RegisterDetach
        // and routes DocumentChanged events back via RaiseCorrection / ValidateElements.
    }

    public void Detach() => _detachAction?.Invoke();

    /// <summary>Called by RevitEventBridge in the Plugin project.</summary>
    public void RaiseCorrection(CorrectionEvent ev)
    {
        _activeCorrections[ev.Id] = ev;
        CorrectionRaised?.Invoke(this, ev);
    }

    /// <summary>Plugin layer registers its own cleanup logic here.</summary>
    public void RegisterDetach(Action detach) => _detachAction = detach;

    /// <summary>Plugin registers a handler that can execute Revit commands for auto-fix.</summary>
    public void RegisterAutoFixHandler(Func<string, string?, bool> handler) => _autoFixHandler = handler;

    /// <summary>
    /// Loads standards from local cache and updates the in-memory list.
    /// Called by the Plugin after login or when standards change.
    /// </summary>
    public async Task LoadStandardsAsync(string companyId)
    {
        _standards = await _db.GetStandardsAsync(companyId);
        Log.Information("Loaded {Count} company standards for validation", _standards.Count);
    }

    /// <summary>
    /// Validates element names/properties provided by the Plugin layer against loaded standards.
    /// Returns corrections for any violations found.
    /// </summary>
    public List<CorrectionEvent> ValidateElements(List<(string ElementId, string Name, string Category)> elements)
    {
        var corrections = new List<CorrectionEvent>();

        foreach (CompanyStandard standard in _standards.Where(s => s.IsActive))
        {
            foreach ((string elementId, string name, string category) in elements)
            {
                if (!MatchesCategory(standard.Category, category))
                    continue;

                if (string.IsNullOrEmpty(standard.Rule))
                    continue;

                try
                {
                    if (!System.Text.RegularExpressions.Regex.IsMatch(name, standard.Rule))
                    {
                        var ev = new CorrectionEvent
                        {
                            RuleId = standard.Id,
                            Title = standard.Name,
                            Description = $"Elemento '{name}' não atende ao padrão: {standard.Description}",
                            Severity = standard.AlertLevel,
                            CanAutoFix = standard.AutoFix,
                            ElementId = elementId
                        };

                        corrections.Add(ev);
                        RaiseCorrection(ev);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Regex error evaluating standard {StandardId} rule '{Rule}'", standard.Id, standard.Rule);
                }
            }
        }

        return corrections;
    }

    /// <summary>
    /// Runs a full validation pass. Called by StandardsService.ValidateModelAsync().
    /// </summary>
    public List<CorrectionEvent> RunValidation() =>
        _activeCorrections.Values
            .Where(c => !c.IsFixed)
            .ToList();

    /// <summary>
    /// Attempts to auto-fix a correction by delegating to the Plugin's Revit command handler.
    /// </summary>
    public bool TryAutoFix(string correctionEventId)
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
            bool success = _autoFixHandler(ev.ElementId ?? string.Empty, standard?.Rule);
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

    private static bool MatchesCategory(string standardCategory, string elementCategory)
    {
        if (string.IsNullOrEmpty(standardCategory)) return true;
        return elementCategory.Contains(standardCategory, StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        Detach();
        _activeCorrections.Clear();
        GC.SuppressFinalize(this);
    }
}
