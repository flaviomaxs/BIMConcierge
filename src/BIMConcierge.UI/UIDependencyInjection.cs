using BIMConcierge.Core.Interfaces;
using BIMConcierge.UI.Services;
using BIMConcierge.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace BIMConcierge.UI;

public static class UIDependencyInjection
{
    public static IServiceCollection AddUIServices(this IServiceCollection services)
    {
        // Navigation
        services.AddSingleton<INavigationService, NavigationService>();

        // Correction alert pop-ups (subscribes to RevitEventDispatcher.CorrectionRaised)
        services.AddSingleton<CorrectionAlertService>();

        // ViewModels
        services.AddTransient<LoginViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<TutorialViewModel>();
        services.AddTransient<CompanyStandardsViewModel>();
        services.AddTransient<StudentProgressViewModel>();
        services.AddTransient<AchievementsViewModel>();
        services.AddTransient<CorrectionAlertViewModel>();
        services.AddTransient<TutorialLibraryViewModel>();
        return services;
    }
}
