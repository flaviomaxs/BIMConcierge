using BIMConcierge.Core.Models;

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
/// Stub — real Revit API wiring lives in Plugin/RevitEventBridge.cs,
/// which is the only project allowed to reference RevitAPI.dll.
/// </summary>
public sealed class RevitEventDispatcher : IRevitEventDispatcher
{
    public event EventHandler<CorrectionEvent>? CorrectionRaised;

    private Action? _detachAction;

    public void Attach(object uiApplicationObject)
    {
        // Intentionally empty here; Plugin/RevitEventBridge calls RegisterDetach
        // and routes DocumentChanged events back via RaiseCorrection.
    }

    public void Detach() => _detachAction?.Invoke();

    /// <summary>Called by RevitEventBridge in the Plugin project.</summary>
    public void RaiseCorrection(CorrectionEvent ev) =>
        CorrectionRaised?.Invoke(this, ev);

    /// <summary>Plugin layer registers its own cleanup logic here.</summary>
    public void RegisterDetach(Action detach) => _detachAction = detach;

    public void Dispose()
    {
        Detach();
        GC.SuppressFinalize(this);
    }
}
