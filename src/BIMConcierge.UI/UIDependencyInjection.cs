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

        // ViewModels
        services.AddTransient<LoginViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<TutorialViewModel>();
        services.AddTransient<CompanyStandardsViewModel>();
        services.AddTransient<StudentProgressViewModel>();
        services.AddTransient<AchievementsViewModel>();
        return services;
    }
}
