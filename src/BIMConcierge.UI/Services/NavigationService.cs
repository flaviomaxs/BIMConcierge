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

    public void NavigateTo(string windowName)
    {
        System.Windows.Window? window = windowName switch
        {
            "Dashboard"         => _serviceProvider.GetRequiredService<DashboardWindow>(),
            "CompanyStandards"  => _serviceProvider.GetRequiredService<CompanyStandardsWindow>(),
            "StudentProgress"   => _serviceProvider.GetRequiredService<StudentProgressWindow>(),
            "Achievements"      => _serviceProvider.GetRequiredService<AchievementsWindow>(),
            "Tutorials"         => _serviceProvider.GetRequiredService<TutorialWindow>(),
            _ => null
        };
        window?.Show();
    }
}
