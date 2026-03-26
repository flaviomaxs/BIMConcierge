using System.Windows;
using System.Windows.Controls;
using BIMConcierge.Core.Models;
using BIMConcierge.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace BIMConcierge.UI.Views.Sections;

public partial class CorrectionAlertView : UserControl
{
    private readonly CorrectionAlertViewModel _vm;

    public event Action? DismissRequested;

    public CorrectionAlertView()
    {
        InitializeComponent();
        _vm = ServiceLocator.ServiceProvider!
            .GetRequiredService<CorrectionAlertViewModel>();
        DataContext = _vm;

        _vm.OnDismiss = () => DismissRequested?.Invoke();
    }

    public void Initialize(CorrectionEvent correction)
    {
        _vm.Initialize(correction);
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        DismissRequested?.Invoke();
    }
}
