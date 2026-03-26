using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace BIMConcierge.Plugin.Commands;

/// <summary>
/// Opens the BIMConcierge main window.
/// Login is handled as an overlay inside DashboardWindow — no separate LoginWindow.
/// </summary>
[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public class OpenDashboardCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        try
        {
            DashboardWindowHelper.ShowAndNavigate(commandData);
            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            message = ex.Message;
            return Result.Failed;
        }
    }
}
