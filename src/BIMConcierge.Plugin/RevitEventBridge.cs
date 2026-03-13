using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;
using BIMConcierge.Core.Models;
using BIMConcierge.Infrastructure.Revit;

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
    }

    private void Detach()
    {
        if (_uiApp is null) return;
        _uiApp.Application.DocumentChanged -= OnDocumentChanged;
        _uiApp = null;
    }

    private void OnDocumentChanged(object? sender, DocumentChangedEventArgs e)
    {
        // TODO: run company-standard rules against e.GetModifiedElementIds()
        // For each violation found, call:
        // _dispatcher.RaiseCorrection(new CorrectionEvent { ... });
    }

    public void Dispose() => Detach();
}
