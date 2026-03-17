using Autodesk.Revit.UI;
using BIMConcierge.Plugin.Commands;

namespace BIMConcierge.Plugin.Ribbon;

/// <summary>
/// Builds the "BIMConcierge" ribbon tab in Revit 2026.
/// </summary>
internal static class RibbonBuilder
{
    private const string TabName   = "BIMConcierge";
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
            ToolTip        = "Abre o painel principal do BIMConcierge",
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

        // ── Company Standards button ───────────────────────────────────────
        var standardsData = new PushButtonData(
            "OpenCompanyStandards",
            "Padrões da\nEmpresa",
            typeof(OpenCompanyStandardsCommand).Assembly.Location,
            typeof(OpenCompanyStandardsCommand).FullName!)
        {
            ToolTip = "Abre a tela de padrões BIM da empresa",
            LongDescription = "Gerencie regras de nomenclatura, LOD, worksets e boas práticas do seu projeto.",
        };

        // ── Student Progress button ────────────────────────────────────────
        var progressData = new PushButtonData(
            "OpenStudentProgress",
            "Progresso do\nAluno",
            typeof(OpenStudentProgressCommand).Assembly.Location,
            typeof(OpenStudentProgressCommand).FullName!)
        {
            ToolTip = "Abre a tela de progresso e habilidades do aluno",
            LongDescription = "Acompanhe tutoriais em andamento, conquistas recentes e proficiência por categoria.",
        };

        panel.AddItem(dashboardData);
        panel.AddSeparator();
        panel.AddItem(tutorialData);
        panel.AddSeparator();
        panel.AddItem(standardsData);
        panel.AddItem(progressData);
    }
}
