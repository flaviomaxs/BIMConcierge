using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using BIMConcierge.UI.Localization;
using BIMConcierge.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace BIMConcierge.UI.Views.Sections;

public partial class LoginSectionView : UserControl
{
    /// <summary>
    /// Raised when the user successfully logs in.
    /// DashboardWindow subscribes to this to hide the login overlay and show the shell.
    /// </summary>
    public event Action? LoginSucceeded;

    public LoginSectionView()
    {
        InitializeComponent();
        var vm = ServiceLocator.ServiceProvider!
            .GetRequiredService<LoginViewModel>();
        DataContext = vm;

        // Marshal onto the UI thread — the async login chain may resume
        // on a thread-pool thread due to ConfigureAwait(false) in BimApiClient.
        vm.OnLoginSuccess = () =>
            Dispatcher.BeginInvoke(() => LoginSucceeded?.Invoke());
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel vm && sender is PasswordBox pb)
            vm.Password = pb.Password;
    }

    private void ForgotPassword_Click(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        MessageBox.Show(
            TranslationSource.GetString("ForgotPasswordMessage"),
            TranslationSource.GetString("LoginForgotPassword"),
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void ContactSupport_Click(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        Process.Start(new ProcessStartInfo("mailto:contato@bimconcierge.io") { UseShellExecute = true });
    }

    private void SignUp_Click(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        Process.Start(new ProcessStartInfo("https://bimconcierge.onrender.com/#planos") { UseShellExecute = true });
    }
}
