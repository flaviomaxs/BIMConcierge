using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;
using BIMConcierge.Infrastructure.Revit;
using Serilog;

namespace BIMConcierge.Plugin;

/// <summary>
/// Bridges Revit API events to the Infrastructure's RevitEventDispatcher.
/// Lives in BIMConcierge.Plugin — the only project that references RevitAPI.dll.
/// Uses ExternalEvent for thread-safe auto-fix from the WPF UI thread.
/// </summary>
public sealed class RevitEventBridge : IDisposable
{
    private readonly RevitEventDispatcher _dispatcher;
    private UIApplication?               _uiApp;
    private AutoFixExternalEventHandler?  _autoFixHandler;
    private ExternalEvent?                _autoFixEvent;

    public RevitEventBridge(RevitEventDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    /// <summary>
    /// Attaches to Revit events only on the first call. Safe to call multiple times.
    /// </summary>
    public void AttachIfNeeded(UIApplication uiApp)
    {
        if (_uiApp is not null) return;
        Attach(uiApp);
    }

    private void Attach(UIApplication uiApp)
    {
        _uiApp = uiApp;
        uiApp.Application.DocumentChanged += OnDocumentChanged;
        _dispatcher.RegisterDetach(() => Detach());

        // Create ExternalEvent for thread-safe auto-fix
        _autoFixHandler = new AutoFixExternalEventHandler();
        _autoFixEvent = ExternalEvent.Create(_autoFixHandler);

        // Register async auto-fix handler that enqueues via ExternalEvent
        _dispatcher.RegisterAutoFixHandler(HandleAutoFixAsync);

        Log.Information("RevitEventBridge attached to DocumentChanged with ExternalEvent auto-fix");
    }

    private void Detach()
    {
        if (_uiApp is null) return;
        _uiApp.Application.DocumentChanged -= OnDocumentChanged;
        _autoFixEvent?.Dispose();
        _autoFixEvent = null;
        _autoFixHandler = null;
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
    /// Enqueues an auto-fix request via ExternalEvent so it executes on the Revit main thread.
    /// Returns a Task that completes when Revit processes the fix.
    /// </summary>
    private Task<bool> HandleAutoFixAsync(string elementIdStr, string? rule)
    {
        if (_autoFixHandler is null || _autoFixEvent is null)
            return Task.FromResult(false);

        var request = new AutoFixRequest { ElementId = elementIdStr, Rule = rule };
        _autoFixHandler.Enqueue(request);
        _autoFixEvent.Raise();

        return request.Completion.Task;
    }

    /// <summary>
    /// Extracts a literal prefix from a regex pattern.
    /// E.g., "^PRJ-.*$" → "PRJ-", "^MEP_" → "MEP_"
    /// </summary>
    internal static string ExtractPrefixFromRule(string rule)
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
