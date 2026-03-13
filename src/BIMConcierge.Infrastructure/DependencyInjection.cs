using BIMConcierge.Core.Interfaces;
using BIMConcierge.Infrastructure.Auth;
using BIMConcierge.Infrastructure.Api;
using BIMConcierge.Infrastructure.License;
using BIMConcierge.Infrastructure.Persistence;
using BIMConcierge.Infrastructure.Revit;
using Microsoft.Extensions.DependencyInjection;

namespace BIMConcierge.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // HTTP client for backend API
        services.AddHttpClient<IBimApiClient, BimApiClient>(client =>
        {
            client.BaseAddress = new Uri(ApiSettings.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // Core services
        services.AddSingleton<IAuthService, AuthService>();
        services.AddSingleton<ILicenseService, LicenseService>();
        services.AddSingleton<ITokenStore, TokenStore>();

        services.AddScoped<ITutorialService, TutorialService>();
        services.AddScoped<IProgressService, ProgressService>();
        services.AddScoped<IStandardsService, StandardsService>();

        // Revit-specific
        services.AddSingleton<IRevitEventDispatcher, RevitEventDispatcher>();

        // Local cache / offline persistence
        services.AddSingleton<ILocalDatabase, SqliteDatabase>();

        return services;
    }
}
