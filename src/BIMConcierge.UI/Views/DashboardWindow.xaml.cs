using BIMConcierge.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Input;

namespace BIMConcierge.UI.Views;

public partial class DashboardWindow : Window
{
    private readonly IServiceProvider _sp;

    public DashboardWindow(IServiceProvider sp)
    {
        _sp = sp;
        InitializeComponent();

        var vm = sp.GetRequiredService<DashboardViewModel>();
        DataContext = vm;

        Loaded += async (_, _) => await vm.LoadCommand.ExecuteAsync(null);
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) =>
        DragMove();

    private void CloseButton_Click(object sender, RoutedEventArgs e) =>
        Close();
}
