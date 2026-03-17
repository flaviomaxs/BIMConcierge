using BIMConcierge.Core.Interfaces;
using BIMConcierge.UI.Views;
using Microsoft.Extensions.DependencyInjection;

namespace BIMConcierge.UI.Services;

/// <summary>
/// Resolves and shows windows from the DI container by name.
/// Registered as singleton since it holds the IServiceProvider reference.
/// </summary>
public sealed class NavigationService : INavigationService
{
    private readonly IServiceProvider _serviceProvider;

    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void NavigateTo(string windowName) => NavigateTo(windowName, null);

    public void NavigateTo(string windowName, object? parameter)
    {
        System.Windows.Window? window = windowName switch
        {
            "Dashboard"         => _serviceProvider.GetRequiredService<DashboardWindow>(),
            "CompanyStandards"  => _serviceProvider.GetRequiredService<CompanyStandardsWindow>(),
            "StudentProgress"   => _serviceProvider.GetRequiredService<StudentProgressWindow>(),
            "Achievements"      => _serviceProvider.GetRequiredService<AchievementsWindow>(),
            "Tutorials"         => _serviceProvider.GetRequiredService<TutorialWindow>(),
            "TutorialLibrary"   => _serviceProvider.GetRequiredService<TutorialLibraryWindow>(),
            "GuidedTutorial"    => CreateWindowWithTutorial<GuidedTutorialWindow>(parameter as string),
            "TutorialDetail"    => CreateWindowWithTutorial<TutorialDetailWindow>(parameter as string),
            _ => null
        };
        window?.Show();
    }

    private T CreateWindowWithTutorial<T>(string? tutorialId) where T : System.Windows.Window
    {
        var window = _serviceProvider.GetRequiredService<T>();
        if (!string.IsNullOrEmpty(tutorialId))
        {
            if (window is GuidedTutorialWindow guided)
                guided.InitializeTutorial(tutorialId);
            else if (window is TutorialDetailWindow detail)
                detail.InitializeTutorial(tutorialId);
        }
        return window;
    }
}
