using System.Windows;
using System.Windows.Input;
using BIMConcierge.UI.ViewModels;

namespace BIMConcierge.UI.Views;

public partial class TutorialLibraryWindow : Window
{
    private readonly TutorialLibraryViewModel _vm;

    public TutorialLibraryWindow(TutorialLibraryViewModel viewModel)
    {
        InitializeComponent();
        _vm = viewModel;
        DataContext = viewModel;
        Loaded += (_, _) => viewModel.LoadCommand.Execute(null);
    }

    private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
            this.DragMove();
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e) => this.Close();

    private void SidebarDashboard_Click(object sender, MouseButtonEventArgs e) =>
        _vm.OpenWindowCommand.Execute("Dashboard");

    private void SidebarStandards_Click(object sender, MouseButtonEventArgs e) =>
        _vm.OpenWindowCommand.Execute("CompanyStandards");

    private void SidebarSettings_Click(object sender, MouseButtonEventArgs e)
    {
        _vm.OpenWindowCommand.Execute("Settings");
        this.Close();
    }
}
