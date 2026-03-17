using System.Windows;
using System.Windows.Input;
using BIMConcierge.Core.Models;
using BIMConcierge.UI.ViewModels;

namespace BIMConcierge.UI.Views;

public partial class CorrectionAlertWindow : Window
{
    private readonly CorrectionAlertViewModel _vm;

    public CorrectionAlertWindow(CorrectionAlertViewModel viewModel)
    {
        InitializeComponent();
        _vm = viewModel;
        DataContext = viewModel;

        viewModel.OnDismiss = () => this.Close();

        this.Loaded += (s, e) =>
        {
            var desktopWorkingArea = SystemParameters.WorkArea;
            this.Left = desktopWorkingArea.Right - this.Width - 20;
            this.Top = desktopWorkingArea.Bottom - this.Height - 20;
        };
    }

    /// <summary>
    /// Called before Show() to populate the ViewModel with the correction event.
    /// </summary>
    public void Initialize(CorrectionEvent correction)
    {
        _vm.Initialize(correction);
    }

    private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
            this.DragMove();
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }
}
