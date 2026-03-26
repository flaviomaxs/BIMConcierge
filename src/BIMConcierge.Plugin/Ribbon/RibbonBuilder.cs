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

    private static PushButton? _tutorialButton;
    private static PushButton? _standardsButton;
    private static PushButton? _progressButton;

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
            LargeImage      = RibbonIcons.Dashboard(32),
            Image            = RibbonIcons.Dashboard(16),
        };

        // ── Tutorial quick-start button ──────────────────────────────────────
        var tutorialData = new PushButtonData(
            "StartTutorial",
            "Iniciar\nTutorial",
            typeof(StartTutorialCommand).Assembly.Location,
            typeof(StartTutorialCommand).FullName!)
        {
            ToolTip    = "Inicia o tutorial guiado diretamente no Revit",
            LargeImage = RibbonIcons.Tutorial(32),
            Image      = RibbonIcons.Tutorial(16),
        };

        // ── Validate Model button ────────────────────────────────────────
        var standardsData = new PushButtonData(
            "OpenCompanyStandards",
            "Validar\nModelo",
            typeof(OpenCompanyStandardsCommand).Assembly.Location,
            typeof(OpenCompanyStandardsCommand).FullName!)
        {
            ToolTip        = "Valida o modelo aberto contra os padrões BIM da empresa",
            LongDescription = "Executa a validação do modelo Revit e exibe as correções encontradas com base nas regras de nomenclatura, LOD, worksets e boas práticas.",
            LargeImage      = RibbonIcons.Standards(32),
            Image            = RibbonIcons.Standards(16),
        };

        // ── Student Progress button ────────────────────────────────────────
        var progressData = new PushButtonData(
            "OpenStudentProgress",
            "Progresso do\nAluno",
            typeof(OpenStudentProgressCommand).Assembly.Location,
            typeof(OpenStudentProgressCommand).FullName!)
        {
            ToolTip        = "Abre a tela de progresso e habilidades do aluno",
            LongDescription = "Acompanhe tutoriais em andamento, conquistas recentes e proficiência por categoria.",
            LargeImage      = RibbonIcons.Progress(32),
            Image            = RibbonIcons.Progress(16),
        };

        panel.AddItem(dashboardData);
        panel.AddSeparator();
        _tutorialButton = panel.AddItem(tutorialData) as PushButton;
        panel.AddSeparator();
        _standardsButton = panel.AddItem(standardsData) as PushButton;
        _progressButton = panel.AddItem(progressData) as PushButton;

        // Disable feature buttons until user logs in
        SetButtonsEnabled(false);
    }

    /// <summary>
    /// Enables or disables the feature ribbon buttons (Tutorial, Standards, Progress).
    /// Called by AuthStateChanged event after login/logout.
    /// </summary>
    public static void SetButtonsEnabled(bool enabled)
    {
        if (_tutorialButton is not null) _tutorialButton.Enabled = enabled;
        if (_standardsButton is not null) _standardsButton.Enabled = enabled;
        if (_progressButton is not null) _progressButton.Enabled = enabled;
    }
}
