using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BIMConcierge.Core.Interfaces;
using BIMConcierge.Core.Models;
using System.Windows.Media;

namespace BIMConcierge.UI.ViewModels;

public partial class CorrectionAlertViewModel : ObservableObject
{
    private readonly IStandardsService _standards;

    [ObservableProperty] private CorrectionEvent? correction;
    [ObservableProperty] private bool isFixed;
    [ObservableProperty] private bool isBusy;

    /// <summary>Invoked by the window to close itself after dismiss/fix.</summary>
    public Action? OnDismiss { get; set; }

    public string Title       => Correction?.Title       ?? string.Empty;
    public string Description => Correction?.Description ?? string.Empty;
    public string ElementId   => Correction?.ElementId   ?? string.Empty;
    public bool   CanAutoFix  => Correction?.CanAutoFix  ?? false;

    public string SeverityIcon => Correction?.Severity switch
    {
        Severity.Error   => "🛑",
        Severity.Warning => "⚠️",
        _                => "ℹ️"
    };

    public SolidColorBrush SeverityBrush => Correction?.Severity switch
    {
        Severity.Error   => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444")),
        Severity.Warning => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B")),
        _                => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6A7D90"))
    };

    public CorrectionAlertViewModel(IStandardsService standards)
    {
        _standards = standards;
    }

    public void Initialize(CorrectionEvent ev)
    {
        Correction = ev;
        IsFixed    = ev.IsFixed;
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(Description));
        OnPropertyChanged(nameof(ElementId));
        OnPropertyChanged(nameof(CanAutoFix));
        OnPropertyChanged(nameof(SeverityIcon));
        OnPropertyChanged(nameof(SeverityBrush));
    }

    [RelayCommand]
    private async Task AutoFixAsync()
    {
        if (Correction is null || !CanAutoFix) return;
        IsBusy = true;
        try
        {
            bool success = await _standards.AutoFixAsync(Correction.Id);
            if (success)
            {
                IsFixed = true;
                Correction.IsFixed = true;
                // Auto-close after successful fix
                OnDismiss?.Invoke();
            }
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void Dismiss()
    {
        OnDismiss?.Invoke();
    }
}
