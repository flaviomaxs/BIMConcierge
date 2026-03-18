using System.Windows;
using System.Windows.Input;
using BIMConcierge.UI.ViewModels;

namespace BIMConcierge.UI.Views;

public partial class AchievementsWindow : Window
{
    private readonly AchievementsViewModel _vm;

    public AchievementsWindow(AchievementsViewModel viewModel)
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
        _vm.OpenWindowCommand.Execute("Dashboard");
        this.Close();
    }

    private void SidebarStandards_Click(object sender, MouseButtonEventArgs e)
    {
        _vm.OpenWindowCommand.Execute("CompanyStandards");
        this.Close();
    }

    private void SidebarProgress_Click(object sender, MouseButtonEventArgs e)
    {
        _vm.OpenWindowCommand.Execute("StudentProgress");
        this.Close();
    }
}
