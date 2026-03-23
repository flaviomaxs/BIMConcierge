using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BIMConcierge.Core.Interfaces;
using BIMConcierge.Core.Models;
using BIMConcierge.UI.Localization;

namespace BIMConcierge.UI.ViewModels;

public partial class CompanyStandardsViewModel : ObservableObject, IDisposable
{
    private readonly IStandardsService   _standards;
    private readonly IAuthService        _auth;
    private readonly INavigationService  _navigation;

    private CancellationTokenSource _cts = new();

    [ObservableProperty] private bool    _isBusy;
    [ObservableProperty] private bool    _isAutoCorrectionEnabled = true;
    [ObservableProperty] private string  _selectedCategory = "Naming Conventions";
    [ObservableProperty] private CompanyStandard? _selectedStandard;
    [ObservableProperty] private string  _errorMessage = string.Empty;

    public ObservableCollection<CompanyStandard> Standards  { get; } = [];
    public ObservableCollection<string>          Categories { get; } =
    [
        "Naming Conventions",
        "LOD Requirements",
        "Workset Rules",
        "Best Practices"
    ];

    public CompanyStandardsViewModel(IStandardsService standards, IAuthService auth, INavigationService navigation)
    {
        _standards  = standards;
        _auth       = auth;
        _navigation = navigation;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        CancelPending();
        CancellationToken ct = _cts.Token;

        IsBusy = true;
        try
        {
            ErrorMessage = string.Empty;
            string companyId = _auth.CurrentUser?.CompanyId ?? string.Empty;
            List<CompanyStandard> list = await _standards.GetStandardsAsync(companyId);
            ct.ThrowIfCancellationRequested();

            Standards.Clear();
            foreach (CompanyStandard s in list) Standards.Add(s);
        }
        catch (Exception ex)
        {
            ErrorMessage = TranslationSource.Format("StandardsLoadError", ex.Message);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task SaveStandardAsync(CompanyStandard standard)
    {
        IsBusy = true;
        try
        {
            await _standards.SaveStandardAsync(standard);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task DeleteStandardAsync(CompanyStandard standard)
    {
        IsBusy = true;
        try
        {
            await _standards.DeleteStandardAsync(standard.Id);
            Standards.Remove(standard);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task RunValidationAsync()
    {
        IsBusy = true;
        try
        {
            await _standards.ValidateModelAsync();
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void SelectCategory(string category) => SelectedCategory = category;

    [RelayCommand]
    private void OpenWindow(string windowName) => _navigation.NavigateTo(windowName);

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
