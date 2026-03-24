using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using BIMConcierge.Core.Interfaces;
using BIMConcierge.Infrastructure.Revit;
using BIMConcierge.UI.Views;
using Microsoft.Extensions.DependencyInjection;

namespace BIMConcierge.Plugin.Commands;

/// <summary>
/// Opens the BIMConcierge main window (Login → Dashboard).
/// Registered as an ExternalCommand in the .addin manifest.
/// </summary>
[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public class OpenDashboardCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        try
        {
            var sp = BIMConciergeApplication.ServiceProvider
                     ?? throw new InvalidOperationException("ServiceProvider não inicializado.");

            var authService = sp.GetRequiredService<IAuthService>();

            // Check local state only (no HTTP calls) — keeps Execute() non-blocking
            if (!authService.IsAuthenticated)
            {
                var loginWindow = sp.GetRequiredService<LoginWindow>();
                var result = loginWindow.ShowDialog();
                if (result != true)
                    return Result.Succeeded;
            }

            if (authService.IsAuthenticated)
            {
                // Attach bridge on first command execution (needs UIApplication)
                var bridge = sp.GetRequiredService<RevitEventBridge>();
                bridge.AttachIfNeeded(commandData.Application);

                var dashboard = sp.GetRequiredService<DashboardWindow>();
                dashboard.Show();
                // Session revalidation, standards loading, and data loading
                // all happen asynchronously inside DashboardViewModel.LoadAsync()
            }

            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            message = ex.Message;
            return Result.Failed;
        }
    }
}
