using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace BIMConcierge.Plugin.Commands;

/// <summary>
/// Opens the DashboardWindow and navigates to the Tutorials section.
/// If the user is not logged in, the login overlay will appear first.
/// </summary>
[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public class StartTutorialCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        try
        {
            DashboardWindowHelper.ShowAndNavigate(commandData, "Tutorials");
            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            message = ex.Message;
            return Result.Failed;
        }
    }
}
