using Autodesk.Revit.UI;
using BIMConcierge.Infrastructure;
using BIMConcierge.Plugin.Ribbon;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace BIMConcierge.Plugin;

/// <summary>
/// BIM Concierge external application — registered in .addin manifest.
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
            Log.Information("BIM Concierge starting up — Revit 2026 | Visual Studio 2026");

            ServiceProvider = BuildServiceProvider();
            RibbonBuilder.Build(application);

            Log.Information("BIM Concierge loaded successfully");
            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "BIM Concierge failed to load");
            TaskDialog.Show("BIM Concierge", $"Falha ao carregar o plugin:\n{ex.Message}");
            return Result.Failed;
        }
    }

    public Result OnShutdown(UIControlledApplication application)
    {
        Log.Information("BIM Concierge shutting down");
        Log.CloseAndFlush();
        return Result.Succeeded;
    }

    // ── Bootstrapping ────────────────────────────────────────────────────────

    private static IServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddInfrastructure();
        // UI services registered on demand (WPF windows are not singletons)
        return services.BuildServiceProvider();
    }

    private static void ConfigureLogger()
    {
        var logPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "BIMConcierge", "logs", "bimconcierge-.log");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(logPath, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7)
            .CreateLogger();
    }
}
