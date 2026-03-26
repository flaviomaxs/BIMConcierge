using BIMConcierge.Core.Interfaces;
using BIMConcierge.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace BIMConcierge.UI.Services;

/// <summary>
/// Navigates between sections inside DashboardWindow by updating
/// DashboardViewModel.ActiveSection. No more standalone windows.
/// </summary>
public sealed class NavigationService : INavigationService
{
    private readonly IServiceProvider _serviceProvider;

    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void NavigateTo(string section) => NavigateTo(section, null);

    public void NavigateTo(string section, object? parameter)
    {
        var dashboardVm = _serviceProvider.GetRequiredService<DashboardViewModel>();

        // Map old window names to new section names for backwards compatibility
        string targetSection = section switch
        {
            "CompanyStandards"  => "Standards",
            "StudentProgress"   => "Progress",
            "TutorialLibrary"   => "Tutorials",
            "Corrections"       => "Corrections",
            "Settings"          => "Settings",
            "GuidedTutorial"    => HandleGuidedTutorial(dashboardVm, parameter as string),
            "TutorialDetail"    => HandleTutorialDetail(dashboardVm, parameter as string),
            _                   => section // Dashboard, Tutorials, Standards, Progress, Achievements, etc.
        };

        dashboardVm.NavigateToCommand.Execute(targetSection);
    }

    private static string HandleGuidedTutorial(DashboardViewModel vm, string? tutorialId)
    {
        if (!string.IsNullOrEmpty(tutorialId))
        {
            vm.GuidedTutorialId = tutorialId;
            vm.IsGuidedTutorialOpen = true;
        }
        return vm.ActiveSection; // Don't change active section
    }

    private static string HandleTutorialDetail(DashboardViewModel vm, string? tutorialId)
    {
        // The TutorialDetailSectionView will pick up the selected tutorial
        return "TutorialDetail";
    }
}
