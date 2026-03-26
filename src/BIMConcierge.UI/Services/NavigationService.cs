using BIMConcierge.Core.Interfaces;
using BIMConcierge.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace BIMConcierge.UI.Services;

/// <summary>
/// Navigates between sections inside DashboardWindow by updating
/// DashboardViewModel.ActiveSection. Resolves the singleton ViewModel lazily
/// to avoid circular DI issues at startup.
/// </summary>
public sealed class NavigationService : INavigationService
{
    private readonly IServiceProvider _serviceProvider;
    private DashboardViewModel? _dashboardVm;

    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    private DashboardViewModel DashboardVm =>
        _dashboardVm ??= _serviceProvider.GetRequiredService<DashboardViewModel>();

    public void NavigateTo(string section) => NavigateTo(section, null);

    public void NavigateTo(string section, object? parameter)
    {
        switch (section)
        {
            case "GuidedTutorial":
                if (parameter is string tutorialId && !string.IsNullOrEmpty(tutorialId))
                {
                    DashboardVm.GuidedTutorialId = tutorialId;
                    DashboardVm.IsGuidedTutorialOpen = true;
                }
                break; // Don't change active section

            case "TutorialDetail":
                DashboardVm.NavigateToCommand.Execute("TutorialDetail");
                break;

            // Map legacy window names to section names
            case "CompanyStandards":
                DashboardVm.NavigateToCommand.Execute("Standards");
                break;
            case "StudentProgress":
                DashboardVm.NavigateToCommand.Execute("Progress");
                break;
            case "TutorialLibrary":
                DashboardVm.NavigateToCommand.Execute("Tutorials");
                break;

            default:
                DashboardVm.NavigateToCommand.Execute(section);
                break;
        }
    }
}
