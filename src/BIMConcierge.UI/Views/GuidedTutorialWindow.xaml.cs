using System.Windows;
using System.Windows.Input;
using BIMConcierge.UI.ViewModels;

namespace BIMConcierge.UI.Views;

public partial class GuidedTutorialWindow : Window
{
    public GuidedTutorialWindow(TutorialViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        this.Loaded += (s, e) =>
        {
            var desktopWorkingArea = SystemParameters.WorkArea;
            this.Left = desktopWorkingArea.Right - this.Width - 50;
            this.Top = desktopWorkingArea.Top + 100;
        };
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
