using Autodesk.Revit.UI;
using BIMConcierge.Infrastructure;
using BIMConcierge.UI;
using BIMConcierge.Plugin.Ribbon;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.IO;

namespace BIMConcierge.Plugin;

/// <summary>
/// BIMConcierge external application — registered in .addin manifest.
/// Loads the ribbon tab and bootstraps the DI container on Revit startup.
/// </summary>
public class BIMConciergeApplication : IExternalApplication
{
    public static IServiceProvider? ServiceProvider { get; private set; }

    public Result OnStartup(UIControlledApplication application)
    {
        try
        {
            ConfigureLogger();
            Log.Information("BIMConcierge starting up — Revit 2026");

            ServiceProvider = BuildServiceProvider();
            RibbonBuilder.Build(application);

            // Eagerly resolve the correction alert service so it subscribes to events immediately
            ServiceProvider.GetRequiredService<BIMConcierge.UI.Services.CorrectionAlertService>();

            Log.Information("BIMConcierge loaded successfully");
            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "BIMConcierge failed to load");
            TaskDialog.Show("BIMConcierge", $"Falha ao carregar o plugin:\n{ex.Message}");
            return Result.Failed;
        }
    }

    public Result OnShutdown(UIControlledApplication application)
    {
        Log.Information("BIMConcierge shutting down");
        Log.CloseAndFlush();
        return Result.Succeeded;
    }

    // ── Bootstrapping ────────────────────────────────────────────────────────

    private static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddInfrastructure();
        services.AddUIServices();

        // Views (require IServiceProvider-free constructors after refactor)
        services.AddTransient<BIMConcierge.UI.Views.LoginWindow>();
        services.AddTransient<BIMConcierge.UI.Views.DashboardWindow>();
        services.AddTransient<BIMConcierge.UI.Views.GuidedTutorialWindow>();
        services.AddTransient<BIMConcierge.UI.Views.TutorialWindow>();
        services.AddTransient<BIMConcierge.UI.Views.TutorialDetailWindow>();
        services.AddTransient<BIMConcierge.UI.Views.CompanyStandardsWindow>();
        services.AddTransient<BIMConcierge.UI.Views.StudentProgressWindow>();
        services.AddTransient<BIMConcierge.UI.Views.AchievementsWindow>();
        services.AddTransient<BIMConcierge.UI.Views.CorrectionAlertWindow>();
        services.AddTransient<BIMConcierge.UI.Views.TutorialLibraryWindow>();

        return services.BuildServiceProvider();
    }

    private static void ConfigureLogger()
    {
        var logPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "BIMConcierge", "logs", "bimconcierge-.log");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                logPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                shared: true,
                flushToDiskInterval: TimeSpan.FromSeconds(5),
                formatProvider: System.Globalization.CultureInfo.InvariantCulture)
            .CreateLogger();
    }
}
