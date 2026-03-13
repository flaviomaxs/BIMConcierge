using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BIMConcierge.Core.Interfaces;

namespace BIMConcierge.UI.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IAuthService    _auth;
    private readonly ILicenseService _license;

    [ObservableProperty] private string  email          = string.Empty;
    [ObservableProperty] private string  password       = string.Empty;
    [ObservableProperty] private string  licenseKey     = string.Empty;
    [ObservableProperty] private bool    rememberMe;
    [ObservableProperty] private bool    isBusy;
    [ObservableProperty] private string? errorMessage;
    [ObservableProperty] private bool    showPassword;

    public event EventHandler? LoginSucceeded;

    public LoginViewModel(IAuthService auth, ILicenseService license)
    {
        _auth    = auth;
        _license = license;
    }

    [RelayCommand(CanExecute = nameof(CanLogin))]
    private async Task LoginAsync()
    {
        ErrorMessage = null;
        IsBusy       = true;
        try
        {
            var result = await _auth.LoginAsync(Email, Password, LicenseKey);
            if (result.Success)
                LoginSucceeded?.Invoke(this, EventArgs.Empty);
            else
                ErrorMessage = result.ErrorMessage ?? "Falha ao autenticar.";
        }
        finally { IsBusy = false; }
    }

    private bool CanLogin() =>
        !IsBusy
        && !string.IsNullOrWhiteSpace(Email)
        && !string.IsNullOrWhiteSpace(Password)
        && !string.IsNullOrWhiteSpace(LicenseKey);

    partial void OnEmailChanged(string value)       => LoginCommand.NotifyCanExecuteChanged();
    partial void OnPasswordChanged(string value)    => LoginCommand.NotifyCanExecuteChanged();
    partial void OnLicenseKeyChanged(string value)  => LoginCommand.NotifyCanExecuteChanged();

    [RelayCommand]
    private void TogglePassword() => ShowPassword = !ShowPassword;
}
