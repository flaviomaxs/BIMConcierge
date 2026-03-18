using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BIMConcierge.Core.Interfaces;
using BIMConcierge.UI.Localization;

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

    [RelayCommand]
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
            var result = await _authService.LoginAsync(Email, Password, LicenseKey);

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
