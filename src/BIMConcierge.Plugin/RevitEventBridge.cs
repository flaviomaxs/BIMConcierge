using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;
using BIMConcierge.Core.Models;
using BIMConcierge.Infrastructure.Revit;
using Serilog;

namespace BIMConcierge.Plugin;

/// <summary>
/// Bridges Revit API events to the Infrastructure's RevitEventDispatcher.
/// Lives in BIMConcierge.Plugin — the only project that references RevitAPI.dll.
/// </summary>
public sealed class RevitEventBridge : IDisposable
{
    private readonly RevitEventDispatcher _dispatcher;
    private UIApplication?               _uiApp;

    public RevitEventBridge(RevitEventDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public void Attach(UIApplication uiApp)
    {
        _uiApp = uiApp;
        uiApp.Application.DocumentChanged += OnDocumentChanged;
        _dispatcher.RegisterDetach(() => Detach());

        // Register auto-fix handler that runs Revit rename in document context
        _dispatcher.RegisterAutoFixHandler(HandleAutoFix);

        Log.Information("RevitEventBridge attached to DocumentChanged");
    }

    private void Detach()
    {
        if (_uiApp is null) return;
        _uiApp.Application.DocumentChanged -= OnDocumentChanged;
        _uiApp = null;
        Log.Information("RevitEventBridge detached");
    }

    private void OnDocumentChanged(object? sender, DocumentChangedEventArgs e)
    {
        try
        {
            Document doc = e.GetDocument();
            ICollection<ElementId> modifiedIds = e.GetModifiedElementIds();
            ICollection<ElementId> addedIds = e.GetAddedElementIds();

            List<ElementId> allIds = modifiedIds.Concat(addedIds).ToList();
            if (allIds.Count == 0) return;

            // Extract element info for validation against company standards
            var elements = new List<(string ElementId, string Name, string Category)>();

            foreach (ElementId id in allIds)
            {
                Element? element = doc.GetElement(id);
                if (element is null) continue;

                string name = element.Name ?? string.Empty;
                string category = element.Category?.Name ?? string.Empty;

                elements.Add((id.ToString(), name, category));
            }

            if (elements.Count > 0)
            {
                _dispatcher.ValidateElements(elements);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error processing DocumentChanged event");
        }
    }

    /// <summary>
    /// Handles auto-fix requests from the dispatcher by renaming elements in the Revit document.
    /// </summary>
    private bool HandleAutoFix(string elementIdStr, string? rule)
    {
        try
        {
            if (_uiApp is null) return false;

            Document? doc = _uiApp.ActiveUIDocument?.Document;
            if (doc is null) return false;

            if (!int.TryParse(elementIdStr, out int idValue))
            {
                // Revit 2026 uses ElementId with long, try long parse
                if (!long.TryParse(elementIdStr, out long longId))
                    return false;
                idValue = (int)longId;
            }

            var elementId = new ElementId(idValue);
            Element? element = doc.GetElement(elementId);
            if (element is null) return false;

            // Apply naming convention from the rule pattern if available
            if (!string.IsNullOrEmpty(rule) && element.Name is not null)
            {
                using var tx = new Transaction(doc, "BIMConcierge — Auto-fix");
                tx.Start();

                // Extract expected prefix from rule pattern (e.g., "^PRJ-" → "PRJ-")
                string prefix = ExtractPrefixFromRule(rule);
                if (!string.IsNullOrEmpty(prefix) && !element.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    element.Name = prefix + element.Name;
                }

                tx.Commit();
                Log.Information("Auto-fix applied: renamed element {Id} with prefix '{Prefix}'", elementIdStr, prefix);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to auto-fix element {ElementId}", elementIdStr);
            return false;
        }
    }

    /// <summary>
    /// Extracts a literal prefix from a regex pattern.
    /// E.g., "^PRJ-.*$" → "PRJ-", "^MEP_" → "MEP_"
    /// </summary>
    private static string ExtractPrefixFromRule(string rule)
    {
        string cleaned = rule.TrimStart('^');
        int prefixEnd = 0;
        foreach (char c in cleaned)
        {
            if (char.IsLetterOrDigit(c) || c == '-' || c == '_')
                prefixEnd++;
            else
                break;
        }
        return cleaned[..prefixEnd];
    }

    public void Dispose() => Detach();
}
