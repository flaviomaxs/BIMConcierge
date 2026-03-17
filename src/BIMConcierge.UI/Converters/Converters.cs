using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace BIMConcierge.UI.Converters;

/// <summary>True → Visible, False/null → Collapsed.</summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is true ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        (Visibility)value == Visibility.Visible;
}

/// <summary>True → Collapsed, False/null → Visible.</summary>
public class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is true ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        (Visibility)value != Visibility.Visible;
}

/// <summary>null/empty string → Collapsed.</summary>
public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is not null && value.ToString() != string.Empty
            ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Maps Severity enum to a SolidColorBrush.</summary>
public class SeverityToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is BIMConcierge.Core.Models.Severity s)
            return s switch
            {
                BIMConcierge.Core.Models.Severity.Error   => System.Windows.Media.Brushes.Crimson,
                BIMConcierge.Core.Models.Severity.Warning => System.Windows.Media.Brushes.Goldenrod,
                _                                         => System.Windows.Media.Brushes.SteelBlue,
            };
        return System.Windows.Media.Brushes.Gray;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Percentage double → GridLength for Width binding.</summary>
public class PercentToGridLengthConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double d) return new GridLength(d, GridUnitType.Star);
        return new GridLength(0);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>
/// Converts a percentage (0–100) to a pixel width.
/// Pass the max width as ConverterParameter (e.g. "300").
/// </summary>
public class PercentToWidthConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        double percent = value switch
        {
            double d => d,
            int i    => i,
            _        => 0
        };
        double maxWidth = 300;
        if (parameter is string s && double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out double parsed))
            maxWidth = parsed;
        return Math.Clamp(percent / 100.0 * maxWidth, 0, maxWidth);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
