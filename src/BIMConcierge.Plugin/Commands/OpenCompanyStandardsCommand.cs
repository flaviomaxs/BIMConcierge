using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using BIMConcierge.Core.Interfaces;
using BIMConcierge.UI.Views;
using Microsoft.Extensions.DependencyInjection;

namespace BIMConcierge.Plugin.Commands;

/// <summary>
/// Opens the Company Standards management window.
/// Requires the user to be authenticated first.
/// </summary>
[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public class OpenCompanyStandardsCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        try
        {
            IServiceProvider sp = BIMConciergeApplication.ServiceProvider
                     ?? throw new InvalidOperationException("ServiceProvider não inicializado.");

            IAuthService authService = sp.GetRequiredService<IAuthService>();

            if (!authService.IsAuthenticated)
            {
                TaskDialog.Show("BIMConcierge",
                    "Você precisa fazer login antes de acessar os Padrões da Empresa.\n" +
                    "Clique em 'Abrir Concierge' para acessar a tela de login.");
                return Result.Cancelled;
            }

            CompanyStandardsWindow window = sp.GetRequiredService<CompanyStandardsWindow>();
            window.Show();
            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            message = ex.Message;
            return Result.Failed;
        }
    }
}
