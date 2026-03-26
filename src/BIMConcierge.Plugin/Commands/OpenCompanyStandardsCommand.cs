using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using BIMConcierge.Core.Interfaces;
using BIMConcierge.Core.Models;
using Microsoft.Extensions.DependencyInjection;

namespace BIMConcierge.Plugin.Commands;

/// <summary>
/// Validates the current Revit model against company BIM standards.
/// Shows a summary dialog and opens the Corrections section if issues are found.
/// </summary>
[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public class OpenCompanyStandardsCommand : IExternalCommand
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
                DashboardWindowHelper.ShowAndNavigate(commandData);
                return Result.Succeeded;
            }

            var standards = sp.GetRequiredService<IStandardsService>();

            // Run validation synchronously on the Revit thread
            List<CorrectionEvent> corrections = standards.ValidateModelAsync()
                .GetAwaiter().GetResult();

            if (corrections.Count == 0)
            {
                TaskDialog.Show("BIMConcierge",
                    "Nenhuma correção encontrada.\nO modelo está em conformidade com os padrões da empresa.");
            }
            else
            {
                TaskDialog.Show("BIMConcierge",
                    $"{corrections.Count} correção(ões) encontrada(s).\nAbrindo o painel de correções...");

                DashboardWindowHelper.ShowAndNavigate(commandData, "Corrections");
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
