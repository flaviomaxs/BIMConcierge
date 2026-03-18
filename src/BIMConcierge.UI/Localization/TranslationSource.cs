using System.ComponentModel;
using System.Globalization;
using System.Resources;

namespace BIMConcierge.UI.Localization;

/// <summary>
/// Singleton that wraps the Strings ResourceManager and raises PropertyChanged
/// when the culture changes, so all XAML bindings update automatically.
/// </summary>
public sealed class TranslationSource : INotifyPropertyChanged
{
    public static TranslationSource Instance { get; } = new();

    private static readonly ResourceManager Rm =
        new("BIMConcierge.UI.Localization.Strings",
            typeof(TranslationSource).Assembly);

    private CultureInfo _currentCulture = CultureInfo.CurrentUICulture;

    private TranslationSource() { }

    /// <summary>
    /// Indexer used by XAML bindings:
    /// <c>Text="{Binding [LoginTitle], Source={x:Static loc:TranslationSource.Instance}}"</c>
    /// </summary>
    public string this[string key] =>
        Rm.GetString(key, _currentCulture) ?? $"[{key}]";

    /// <summary>
    /// Gets a formatted string with arguments.
    /// </summary>
    public static string GetString(string key) =>
        Rm.GetString(key, Instance._currentCulture) ?? $"[{key}]";

    /// <summary>
    /// Gets a formatted string with arguments.
    /// </summary>
    public static string Format(string key, params object[] args) =>
        string.Format(GetString(key), args);

    public CultureInfo CurrentCulture
    {
        get => _currentCulture;
        set
        {
            if (Equals(_currentCulture, value)) return;
            _currentCulture = value;
            // "Item[]" triggers refresh for all indexer bindings
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
            CultureChanged?.Invoke(this, value);
        }
    }

    /// <summary>
    /// Switches to the given culture code (e.g., "pt-BR", "en").
    /// </summary>
    public void SetCulture(string cultureCode)
    {
        CurrentCulture = new CultureInfo(cultureCode);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Raised after culture changes, so ViewModels can refresh formatted strings.
    /// </summary>
    public static event EventHandler<CultureInfo>? CultureChanged;
}
