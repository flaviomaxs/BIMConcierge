using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BIMConcierge.Core.Interfaces;
using BIMConcierge.Core.Models;
using BIMConcierge.UI.Localization;

namespace BIMConcierge.UI.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IAuthService       _auth;
    private readonly INavigationService _navigation;

    [ObservableProperty] private string _selectedLanguage = "pt-BR";
    [ObservableProperty] private string _currentTheme     = "Dark";
    [ObservableProperty] private string _userInitials     = string.Empty;
    [ObservableProperty] private string _userName         = string.Empty;
    [ObservableProperty] private string _userEmail        = string.Empty;
    [ObservableProperty] private string _userRole         = string.Empty;
    [ObservableProperty] private string _planName         = string.Empty;
    [ObservableProperty] private string _appVersion       = "1.0.0";

    public string[] AvailableLanguages { get; } = ["pt-BR", "en"];
    public string[] AvailableThemes    { get; } = ["Dark"];

    public SettingsViewModel(IAuthService auth, INavigationService navigation)
    {
        _auth       = auth;
        _navigation = navigation;
    }

    [RelayCommand]
    private void Load()
    {
        User? user = _auth.CurrentUser;
        if (user is not null)
        {
            UserInitials = user.Initials;
            UserName     = user.Name;
            UserEmail    = user.Email;
            UserRole     = user.Role;
        }

        License? license = _auth.CurrentLicense;
        PlanName = license?.Type.ToString() ?? "—";

        SelectedLanguage = CultureInfo.CurrentUICulture.Name switch
        {
            "en" or "en-US" or "en-GB" => "en",
            _ => "pt-BR"
        };
    }

    [RelayCommand]
    private void ChangeLanguage(string lang)
    {
        SelectedLanguage = lang;
        TranslationSource.Instance.SetCulture(lang);
    }

    [RelayCommand]
    private void OpenWindow(string windowName) => _navigation.NavigateTo(windowName);
}
