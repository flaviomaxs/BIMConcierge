using System.Windows;
using System.Windows.Input;
using BIMConcierge.UI.ViewModels;

namespace BIMConcierge.UI.Views;

public partial class DashboardWindow : Window
{
    private readonly DashboardViewModel _vm;

    public DashboardWindow(DashboardViewModel viewModel)
    {
        InitializeComponent();
        _vm = viewModel;
        DataContext = viewModel;
    }

    private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
            this.DragMove();
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e) => this.Close();

    private void SidebarTutorials_Click(object sender, MouseButtonEventArgs e) =>
        _vm.OpenWindowCommand.Execute("TutorialLibrary");

    private void SidebarCorrections_Click(object sender, MouseButtonEventArgs e) =>
        _vm.OpenWindowCommand.Execute("Corrections");

    private void SidebarStandards_Click(object sender, MouseButtonEventArgs e) =>
        _vm.OpenWindowCommand.Execute("CompanyStandards");

    private void SidebarProgress_Click(object sender, MouseButtonEventArgs e) =>
        _vm.OpenWindowCommand.Execute("StudentProgress");

    private void SidebarAchievements_Click(object sender, MouseButtonEventArgs e) =>
        _vm.OpenWindowCommand.Execute("Achievements");
}
