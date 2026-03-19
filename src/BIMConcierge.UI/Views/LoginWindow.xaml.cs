using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BIMConcierge.UI.Localization;
using BIMConcierge.UI.ViewModels;

namespace BIMConcierge.UI.Views;

public partial class LoginWindow : Window
{
    public LoginWindow(LoginViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        viewModel.OnLoginSuccess = () =>
        {
            this.DialogResult = true;
            this.Close();
        };
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel vm && sender is PasswordBox pb)
            vm.Password = pb.Password;
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

    private void ForgotPassword_Click(object sender, MouseButtonEventArgs e)
    {
        MessageBox.Show(
            TranslationSource.GetString("ForgotPasswordMessage"),
            TranslationSource.GetString("LoginForgotPassword"),
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void ContactSupport_Click(object sender, MouseButtonEventArgs e)
    {
        Process.Start(new ProcessStartInfo("mailto:contato@bimconcierge.io") { UseShellExecute = true });
    }
}
