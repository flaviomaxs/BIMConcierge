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
