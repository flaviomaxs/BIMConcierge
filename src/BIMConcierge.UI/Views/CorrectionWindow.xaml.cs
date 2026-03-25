using System.Windows;
using System.Windows.Input;
using BIMConcierge.UI.ViewModels;

namespace BIMConcierge.UI.Views;

public partial class CorrectionWindow : Window
{
    private readonly CorrectionViewModel _vm;

    public CorrectionWindow(CorrectionViewModel viewModel)
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

    private void SidebarDashboard_Click(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        _vm.OpenWindowCommand.Execute("Dashboard");
    }

    private void SidebarTutorials_Click(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        _vm.OpenWindowCommand.Execute("TutorialLibrary");
    }

    private void SidebarStandards_Click(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        _vm.OpenWindowCommand.Execute("CompanyStandards");
    }

    private void SidebarProgress_Click(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        _vm.OpenWindowCommand.Execute("StudentProgress");
    }

    private void SidebarAchievements_Click(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        _vm.OpenWindowCommand.Execute("Achievements");
    }
}
