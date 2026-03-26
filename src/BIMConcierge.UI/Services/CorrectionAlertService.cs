using BIMConcierge.Core.Models;
using BIMConcierge.Infrastructure.Revit;
using BIMConcierge.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace BIMConcierge.UI.Services;

/// <summary>
/// Listens to CorrectionRaised events from the RevitEventDispatcher
/// and shows the CorrectionAlert overlay inside DashboardWindow.
/// Registered as singleton so it lives for the duration of the Revit session.
/// </summary>
public sealed class CorrectionAlertService : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IRevitEventDispatcher _dispatcher;

    public CorrectionAlertService(IServiceProvider serviceProvider, IRevitEventDispatcher dispatcher)
    {
        _serviceProvider = serviceProvider;
        _dispatcher = dispatcher;
        _dispatcher.CorrectionRaised += OnCorrectionRaised;
    }

    private void OnCorrectionRaised(object? sender, CorrectionEvent ev)
    {
        // CorrectionRaised may fire from a non-UI thread (Revit API callback),
        // so we must dispatch to the WPF thread.
        var wpfDispatcher = System.Windows.Application.Current?.Dispatcher;
        if (wpfDispatcher is null) return;

        wpfDispatcher.BeginInvoke(() => ShowAlert(ev));
    }

    private void ShowAlert(CorrectionEvent ev)
    {
        var dashboardVm = _serviceProvider.GetRequiredService<DashboardViewModel>();
        dashboardVm.ShowCorrectionAlertCommand.Execute(ev);
    }

    public void Dispose()
    {
        _dispatcher.CorrectionRaised -= OnCorrectionRaised;
    }
}
