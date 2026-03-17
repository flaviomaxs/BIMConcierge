using System.Windows;
using System.Windows.Input;
using BIMConcierge.UI.ViewModels;

namespace BIMConcierge.UI.Views;

public partial class CompanyStandardsWindow : Window
{
    private readonly CompanyStandardsViewModel _vm;

    public CompanyStandardsWindow(CompanyStandardsViewModel viewModel)
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

    private void SidebarDashboard_Click(object sender, MouseButtonEventArgs e)
    {
        _vm.OpenWindowCommand.Execute("Dashboard");
        this.Close();
    }

    private void SidebarProgress_Click(object sender, MouseButtonEventArgs e)
    {
        _vm.OpenWindowCommand.Execute("StudentProgress");
        this.Close();
    }

    private void SidebarAchievements_Click(object sender, MouseButtonEventArgs e)
    {
        _vm.OpenWindowCommand.Execute("Achievements");
        this.Close();
    }
}
