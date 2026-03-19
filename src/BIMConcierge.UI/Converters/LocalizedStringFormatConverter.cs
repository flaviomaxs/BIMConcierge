using System.Globalization;
using System.Windows.Data;
using BIMConcierge.UI.Localization;

namespace BIMConcierge.UI.Converters;

/// <summary>
/// Formats a bound value using a localized string pattern.
/// Usage: set <c>Key</c> property or pass the resource key as <c>ConverterParameter</c>.
/// Example: <c>Converter={StaticResource LocalizedFormat} ConverterParameter="TutorialsSteps"</c>
/// </summary>
public class LocalizedStringFormatConverter : IValueConverter
{
    public string Key { get; set; } = "";

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        string key = parameter as string ?? Key;

        if (string.IsNullOrEmpty(key) || value is null)
            return value ?? "";

        return TranslationSource.Format(key, value);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
