using System.Windows;
using System.Windows.Input;
using BIMConcierge.UI.ViewModels;

namespace BIMConcierge.UI.Views;

public partial class TutorialDetailWindow : Window
{
    private readonly TutorialViewModel _vm;

    public TutorialDetailWindow(TutorialViewModel viewModel)
    {
        InitializeComponent();
        _vm = viewModel;
        DataContext = viewModel;
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
