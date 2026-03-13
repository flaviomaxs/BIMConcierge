using BIMConcierge.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Input;

namespace BIMConcierge.UI.Views;

/// <summary>
/// Step-by-step guided tutorial window.
/// </summary>
public partial class TutorialWindow : Window
{
    public TutorialWindow(IServiceProvider sp)
    {
        InitializeComponent();
        DataContext = sp.GetRequiredService<TutorialViewModel>();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) =>
        DragMove();

    private void CloseButton_Click(object sender, RoutedEventArgs e) =>
        Close();
}
