using System.Windows;
using System.Windows.Input;
using BIMConcierge.UI.ViewModels;

namespace BIMConcierge.UI.Views;

public partial class GuidedTutorialWindow : Window
{
    private readonly TutorialViewModel _vm;

    public GuidedTutorialWindow(TutorialViewModel viewModel)
    {
        InitializeComponent();
        _vm = viewModel;
        DataContext = viewModel;

        this.Loaded += (s, e) =>
        {
            var desktopWorkingArea = SystemParameters.WorkArea;
            this.Left = desktopWorkingArea.Right - this.Width - 50;
            this.Top = desktopWorkingArea.Top + 100;
        };
    }

    /// <summary>
    /// Called by NavigationService to load a specific tutorial before showing the window.
    /// </summary>
    public async void InitializeTutorial(string tutorialId)
    {
        await _vm.LoadTutorialAsync(tutorialId);
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
