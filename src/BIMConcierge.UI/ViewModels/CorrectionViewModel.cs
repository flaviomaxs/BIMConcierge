using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BIMConcierge.Core.Interfaces;
using BIMConcierge.Core.Models;
using BIMConcierge.Infrastructure.Revit;

namespace BIMConcierge.UI.ViewModels;

public partial class CorrectionViewModel : ObservableObject, IDisposable
{
    private readonly IStandardsService      _standards;
    private readonly IAuthService           _auth;
    private readonly INavigationService     _navigation;
    private readonly IRevitEventDispatcher  _dispatcher;

    private CancellationTokenSource _cts = new();

    [ObservableProperty] private bool   isBusy;
    [ObservableProperty] private string errorMessage      = string.Empty;
    [ObservableProperty] private string selectedSeverity  = "Todos";
    [ObservableProperty] private bool   isAutoFixEnabled;

    /// <summary>All corrections loaded from the validation engine (unfiltered).</summary>
    private readonly List<CorrectionEvent> _allCorrections = [];

    /// <summary>Filtered corrections bound to the UI list.</summary>
    public ObservableCollection<CorrectionEvent> Corrections { get; } = [];

    public int ActiveCount => _allCorrections.Count(c => !c.IsFixed);
    public int FixedCount  => _allCorrections.Count(c => c.IsFixed);

    public CorrectionViewModel(
        IStandardsService standards,
        IAuthService auth,
        INavigationService navigation,
        IRevitEventDispatcher dispatcher)
    {
        _standards  = standards;
        _auth       = auth;
        _navigation = navigation;
        _dispatcher = dispatcher;

        // Sync auto-fix toggle with dispatcher state
        if (_dispatcher is RevitEventDispatcher revit)
            IsAutoFixEnabled = revit.AutoFixOnDetection;
    }

    partial void OnIsAutoFixEnabledChanged(bool value)
    {
        if (_dispatcher is RevitEventDispatcher revit)
            revit.AutoFixOnDetection = value;
    }

    // -- Load -------------------------------------------------------------------

    [RelayCommand]
    private async Task LoadAsync()
    {
        CancelPending();
        var ct = _cts.Token;

        IsBusy = true;
        try
        {
            ErrorMessage = string.Empty;

            var corrections = await _standards.ValidateModelAsync();
            ct.ThrowIfCancellationRequested();

            _allCorrections.Clear();
            _allCorrections.AddRange(corrections);

            ApplyFilter();
            RefreshCounts();
        }
        catch (OperationCanceledException) { /* expected */ }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao carregar correções: {ex.Message}";
        }
        finally { IsBusy = false; }
    }

    // -- Filter -----------------------------------------------------------------

    [RelayCommand]
    private void SetFilter(string severity)
    {
        SelectedSeverity = severity;
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var filtered = _allCorrections.AsEnumerable();

        if (!string.IsNullOrEmpty(SelectedSeverity) && SelectedSeverity != "Todos")
        {
            if (Enum.TryParse<Severity>(SelectedSeverity, true, out var sev))
                filtered = filtered.Where(c => c.Severity == sev);
        }

        Corrections.Clear();
        foreach (var c in filtered)
            Corrections.Add(c);
    }

    // -- Actions ----------------------------------------------------------------

    [RelayCommand]
    private async Task AutoFixAsync(CorrectionEvent correction)
    {
        if (correction is null || !correction.CanAutoFix) return;
        IsBusy = true;
        try
        {
            bool success = await _standards.AutoFixAsync(correction.Id);
            if (success)
            {
                correction.IsFixed = true;
                ApplyFilter();
                RefreshCounts();
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao corrigir: {ex.Message}";
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void Dismiss(CorrectionEvent correction)
    {
        if (correction is null) return;

        // Notify dispatcher so this element+rule won't be raised again
        if (_dispatcher is RevitEventDispatcher revit)
            revit.DismissCorrection(correction.Id);

        _allCorrections.Remove(correction);
        Corrections.Remove(correction);
        RefreshCounts();
    }

    // -- Navigation -------------------------------------------------------------

    [RelayCommand]
    private void OpenWindow(string windowName) => _navigation.NavigateTo(windowName);

    // -- Helpers ----------------------------------------------------------------

    private void RefreshCounts()
    {
        OnPropertyChanged(nameof(ActiveCount));
        OnPropertyChanged(nameof(FixedCount));
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
