using Autodesk.Revit.UI;
using BIMConcierge.Plugin.Commands;

namespace BIMConcierge.Plugin.Ribbon;

/// <summary>
/// Builds the "BIM Concierge" ribbon tab in Revit 2026.
/// </summary>
internal static class RibbonBuilder
{
    private const string TabName   = "BIM Concierge";
    private const string PanelName = "Assistente";

    public static void Build(UIControlledApplication app)
    {
        app.CreateRibbonTab(TabName);
        var panel = app.CreateRibbonPanel(TabName, PanelName);

        // ── Main button — opens Dashboard / Login ────────────────────────────
        var dashboardData = new PushButtonData(
            "OpenDashboard",
            "Abrir\nConcierge",
            typeof(OpenDashboardCommand).Assembly.Location,
            typeof(OpenDashboardCommand).FullName!)
        {
            ToolTip        = "Abre o painel principal do BIM Concierge",
            LongDescription = "Acesse tutoriais guiados, correção em tempo real e os padrões da empresa.",
        };

        // ── Tutorial quick-start button ──────────────────────────────────────
        var tutorialData = new PushButtonData(
            "StartTutorial",
            "Iniciar\nTutorial",
            typeof(StartTutorialCommand).Assembly.Location,
            typeof(StartTutorialCommand).FullName!)
        {
            ToolTip = "Inicia o tutorial guiado diretamente no Revit",
        };

        panel.AddItem(dashboardData);
        panel.AddSeparator();
        panel.AddItem(tutorialData);
    }
}
