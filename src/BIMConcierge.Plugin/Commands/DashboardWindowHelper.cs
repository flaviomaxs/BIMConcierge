using Autodesk.Revit.UI;
using BIMConcierge.Infrastructure.Revit;
using BIMConcierge.UI.ViewModels;
using BIMConcierge.UI.Views;
using Microsoft.Extensions.DependencyInjection;

namespace BIMConcierge.Plugin.Commands;

/// <summary>
/// Manages the single DashboardWindow instance.
/// Ensures only one window exists at a time and provides navigation helpers.
/// </summary>
internal static class DashboardWindowHelper
{
    private static DashboardWindow? _window;

    /// <summary>
    /// Shows the DashboardWindow (creating it if needed) and optionally navigates to a section.
    /// </summary>
    public static void ShowAndNavigate(ExternalCommandData commandData, string? section = null)
    {
        var sp = BIMConciergeApplication.ServiceProvider
                 ?? throw new InvalidOperationException("ServiceProvider não inicializado.");

        // Attach RevitEventBridge on first use (needs UIApplication)
        var bridge = sp.GetRequiredService<RevitEventBridge>();
        bridge.AttachIfNeeded(commandData.Application);

        // Reuse existing window or create a new one
        if (_window is null || !_window.IsLoaded)
        {
            var vm = sp.GetRequiredService<DashboardViewModel>();
            _window = new DashboardWindow(vm);
            _window.Closed += (_, _) => _window = null;
        }

        _window.Show();
        _window.Activate();

        // Navigate to requested section
        if (!string.IsNullOrEmpty(section))
        {
            var dashVm = sp.GetRequiredService<DashboardViewModel>();
            dashVm.NavigateToCommand.Execute(section);
        }
    }
}
