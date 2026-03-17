using BIMConcierge.Core.Models;
using BIMConcierge.Infrastructure.Revit;
using BIMConcierge.UI.Views;
using Microsoft.Extensions.DependencyInjection;

namespace BIMConcierge.UI.Services;

/// <summary>
/// Listens to CorrectionRaised events from the RevitEventDispatcher
/// and shows CorrectionAlertWindow pop-ups on the WPF dispatcher thread.
/// Registered as singleton so it lives for the duration of the Revit session.
/// </summary>
public sealed class CorrectionAlertService : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IRevitEventDispatcher _dispatcher;
    private int _activeAlertCount;
    private const int MaxConcurrentAlerts = 3;

    public CorrectionAlertService(IServiceProvider serviceProvider, IRevitEventDispatcher dispatcher)
    {
        _serviceProvider = serviceProvider;
        _dispatcher = dispatcher;
        _dispatcher.CorrectionRaised += OnCorrectionRaised;
    }

    private void OnCorrectionRaised(object? sender, CorrectionEvent ev)
    {
        if (_activeAlertCount >= MaxConcurrentAlerts) return;

        // CorrectionRaised may fire from a non-UI thread (Revit API callback),
        // so we must dispatch to the WPF thread.
        var wpfDispatcher = System.Windows.Application.Current?.Dispatcher;
        if (wpfDispatcher is null) return;

        wpfDispatcher.BeginInvoke(() => ShowAlert(ev));
    }

    private void ShowAlert(CorrectionEvent ev)
    {
        var window = _serviceProvider.GetRequiredService<CorrectionAlertWindow>();
        window.Initialize(ev);

        // Stack alerts vertically — offset each by its height + margin
        var desktopWorkingArea = System.Windows.SystemParameters.WorkArea;
        double alertHeight = 280; // approximate
        double offset = _activeAlertCount * (alertHeight + 10);

        window.Loaded += (s, e) =>
        {
            window.Left = desktopWorkingArea.Right - window.Width - 20;
            window.Top = desktopWorkingArea.Bottom - window.Height - 20 - offset;
        };

        _activeAlertCount++;
        window.Closed += (s, e) => _activeAlertCount--;

        window.Show();
    }

    public void Dispose()
    {
        _dispatcher.CorrectionRaised -= OnCorrectionRaised;
    }
}
