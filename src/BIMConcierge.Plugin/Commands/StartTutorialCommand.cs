using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using BIMConcierge.Core.Interfaces;
using BIMConcierge.UI.Views;
using Microsoft.Extensions.DependencyInjection;

namespace BIMConcierge.Plugin.Commands;

/// <summary>
/// Directly opens the Guided Tutorial window.
/// Requires the user to be authenticated first.
/// </summary>
[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public class StartTutorialCommand : IExternalCommand
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
                TaskDialog.Show("BIM Concierge",
                    "Você precisa fazer login antes de iniciar um tutorial.\n" +
                    "Clique em 'Abrir Concierge' para acessar a tela de login.");
                return Result.Cancelled;
            }

            var tutorialWindow = new TutorialWindow(sp);
            tutorialWindow.Show();
            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            message = ex.Message;
            return Result.Failed;
        }
    }
}
