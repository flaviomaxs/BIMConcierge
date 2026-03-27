using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BIMConcierge.Core.Interfaces;
using BIMConcierge.UI.Localization;
using Serilog;

namespace BIMConcierge.UI.ViewModels;

public partial class LoginViewModel : ObservableObject, IDisposable
{
    private readonly IAuthService _authService;
    public Action? OnLoginSuccess { get; set; }

    private CancellationTokenSource _cts = new();

    [ObservableProperty] private string _email = string.Empty;
    [ObservableProperty] private string _password = string.Empty;
    [ObservableProperty] private string _licenseKey = string.Empty;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private bool _hasError;
    [ObservableProperty] private bool _isBusy;

    public LoginViewModel(IAuthService authService)
    {
        _authService = authService;
    }

    public string ButtonLabel => IsBusy
        ? (TranslationSource.GetString("LoginLoading") ?? "Entrando...")
        : (TranslationSource.GetString("LoginButton") ?? "Entrar");

    private bool CanLogin() => !IsBusy;

    partial void OnIsBusyChanged(bool value)
    {
        LoginCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(ButtonLabel));
    }

    [RelayCommand(CanExecute = nameof(CanLogin))]
    private async Task LoginAsync()
    {
        HasError = false;
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password) || string.IsNullOrWhiteSpace(LicenseKey))
        {
            ErrorMessage = TranslationSource.GetString("LoginAllFieldsRequired");
            HasError = true;
            return;
        }

        CancelPending();

        IsBusy = true;
        try
        {
            AuthResult result = await _authService.LoginAsync(Email, Password, LicenseKey, _cts.Token);

            if (result.Success)
            {
                OnLoginSuccess?.Invoke();
            }
            else
            {
                ErrorMessage = result.ErrorMessage ?? TranslationSource.GetString("LoginFailed");
                HasError = true;
            }
        }
        catch (OperationCanceledException) when (_cts.IsCancellationRequested)
        {
            // Only swallow if WE triggered cancellation (e.g. user navigated away).
            // HTTP timeouts also throw OperationCanceledException but must surface.
        }
        catch (Exception ex)
        {
            ErrorMessage = TranslationSource.GetString("LoginFailed") ?? "Falha no login.";
            HasError = true;
            Log.Error(ex, "Login failed unexpectedly");
        }
        finally { IsBusy = false; }
    }

    private void CancelPending()
    {
        _cts.Cancel();
        _cts.Dispose();
        _cts = new CancellationTokenSource();
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}
