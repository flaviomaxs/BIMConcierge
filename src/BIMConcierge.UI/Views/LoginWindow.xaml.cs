using BIMConcierge.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Input;

namespace BIMConcierge.UI.Views;

public partial class LoginWindow : Window
{
    private readonly IServiceProvider _sp;

    public LoginWindow(IServiceProvider sp)
    {
        _sp = sp;
        InitializeComponent();

        var vm = sp.GetRequiredService<LoginViewModel>();
        vm.LoginSucceeded += OnLoginSucceeded;
        DataContext = vm;
    }

    private void OnLoginSucceeded(object? sender, EventArgs e)
    {
        var dashboard = new DashboardWindow(_sp);
        dashboard.Show();
        Close();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) =>
        DragMove();

    private void CloseButton_Click(object sender, RoutedEventArgs e) =>
        Close();
}
