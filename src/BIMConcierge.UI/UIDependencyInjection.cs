using BIMConcierge.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace BIMConcierge.UI;

public static class UIDependencyInjection
{
    public static IServiceCollection AddUIServices(this IServiceCollection services)
    {
        services.AddTransient<LoginViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<TutorialViewModel>();
        return services;
    }
}
