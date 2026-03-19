using System.Collections.Concurrent;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Serilog;

namespace BIMConcierge.Plugin;

/// <summary>
/// IExternalEventHandler that processes auto-fix requests on the Revit main thread.
/// Queued requests are executed during Revit's idle time via ExternalEvent.Raise().
/// </summary>
public sealed class AutoFixExternalCommand : IExternalEventHandler
{
    private readonly ConcurrentQueue<AutoFixRequest> _queue = new();

    public void Enqueue(AutoFixRequest request) => _queue.Enqueue(request);

    public void Execute(UIApplication app)
    {
        Document? doc = app.ActiveUIDocument?.Document;
        if (doc is null)
        {
            // Drain queue and fail all requests
            while (_queue.TryDequeue(out var failed))
                failed.Completion.TrySetResult(false);
            return;
        }

        while (_queue.TryDequeue(out var request))
        {
            try
            {
                bool success = ApplyFix(doc, request.ElementId, request.Rule);
                request.Completion.TrySetResult(success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Auto-fix failed for element {ElementId}", request.ElementId);
                request.Completion.TrySetResult(false);
            }
        }
    }

    public string GetName() => "BIMConcierge.AutoFix";

    private static bool ApplyFix(Document doc, string elementIdStr, string? rule)
    {
        if (!int.TryParse(elementIdStr, out int idValue))
        {
            // Revit 2026 uses ElementId with long
            if (!long.TryParse(elementIdStr, out long longId))
                return false;
            idValue = (int)longId;
        }

        var elementId = new ElementId(idValue);
        Element? element = doc.GetElement(elementId);
        if (element is null) return false;

        if (string.IsNullOrEmpty(rule) || element.Name is null)
            return false;

        string prefix = RevitEventBridge.ExtractPrefixFromRule(rule);
        if (string.IsNullOrEmpty(prefix) || element.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return false;

        using var tx = new Transaction(doc, "BIMConcierge — Auto-fix");
        tx.Start();
        element.Name = prefix + element.Name;
        tx.Commit();

        Log.Information("Auto-fix applied: renamed element {Id} with prefix '{Prefix}'", elementIdStr, prefix);
        return true;
    }
}

/// <summary>
/// Represents a pending auto-fix request to be processed by the ExternalEvent handler.
/// </summary>
public sealed class AutoFixRequest
{
    public required string ElementId { get; init; }
    public string? Rule { get; init; }
    public TaskCompletionSource<bool> Completion { get; } = new();
}
