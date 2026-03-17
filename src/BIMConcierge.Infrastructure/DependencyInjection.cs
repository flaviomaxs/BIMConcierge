using BIMConcierge.Core.Interfaces;
using BIMConcierge.Infrastructure.Auth;
using BIMConcierge.Infrastructure.Api;
using BIMConcierge.Infrastructure.Licensing;
using BIMConcierge.Infrastructure.Persistence;
using BIMConcierge.Infrastructure.Revit;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.CircuitBreaker;
using Polly.Extensions.Http;
using Polly.Retry;

namespace BIMConcierge.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // In dev mode, use a fake API client that returns mock data (no HTTP calls)
        if (Environment.GetEnvironmentVariable("BIMCONCIERGE_DEV_MODE") == "true")
        {
            services.AddSingleton<IBimApiClient, DevBimApiClient>();
        }
        else
        {
            // HTTP client with Polly retry + circuit breaker
            services.AddHttpClient<IBimApiClient, BimApiClient>(client =>
            {
                client.BaseAddress = new Uri(ApiSettings.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(90);
            })
            .AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler(GetCircuitBreakerPolicy());
        }

        // Auth & License — transient so each resolution gets a fresh BimApiClient from the factory
        services.AddSingleton<ITokenStore, TokenStore>();
        services.AddTransient<IAuthService, AuthService>();
        services.AddTransient<ILicenseService, LicenseService>();

        // Domain services
        services.AddTransient<ITutorialService, TutorialService>();
        services.AddTransient<IProgressService, ProgressService>();
        services.AddTransient<IStandardsService, StandardsService>();

        // Revit-specific
        services.AddSingleton<IRevitEventDispatcher, RevitEventDispatcher>();

        // Local cache / offline persistence
        services.AddSingleton<ILocalDatabase, SqliteDatabase>();

        return services;
    }

    /// <summary>
    /// Retry up to 3 times with exponential backoff (1s, 2s, 4s) on transient HTTP errors.
    /// </summary>
    private static AsyncRetryPolicy<HttpResponseMessage> GetRetryPolicy() =>
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt - 1)));

    /// <summary>
    /// Break the circuit for 30s after 5 consecutive failures — avoids flooding a down API.
    /// </summary>
    private static AsyncCircuitBreakerPolicy<HttpResponseMessage> GetCircuitBreakerPolicy() =>
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
}
