using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using BIMConcierge.Core.Interfaces;
using BIMConcierge.UI.Views;
using Microsoft.Extensions.DependencyInjection;

namespace BIMConcierge.Plugin.Commands;

/// <summary>
/// Opens the BIM Concierge main window (Login → Dashboard).
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

            if (!authService.IsAuthenticated)
            {
                var loginWindow = new LoginWindow(sp);
                loginWindow.ShowDialog();
            }
            else
            {
                var dashboard = new DashboardWindow(sp);
                dashboard.Show();
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
