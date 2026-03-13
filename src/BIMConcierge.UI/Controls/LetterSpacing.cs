using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace BIMConcierge.UI.Controls;

/// <summary>
/// Attached property that mimics WinUI's CharacterSpacing on WPF TextBlocks.
/// Usage: controls:LetterSpacing.Value="1.5"  (value in pixels, like CSS letter-spacing)
/// </summary>
public static class LetterSpacing
{
    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.RegisterAttached(
            "Value",
            typeof(double),
            typeof(LetterSpacing),
            new PropertyMetadata(0.0, OnValueChanged));

    public static double GetValue(DependencyObject obj) =>
        (double)obj.GetValue(ValueProperty);

    public static void SetValue(DependencyObject obj, double value) =>
        obj.SetValue(ValueProperty, value);

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBlock tb) return;
        tb.Loaded -= ApplySpacing;
        tb.Loaded += ApplySpacing;
        if (tb.IsLoaded) ApplySpacing(tb, null);
    }

    private static void ApplySpacing(object sender, RoutedEventArgs? _)
    {
        if (sender is not TextBlock tb) return;
        var spacing = GetValue(tb);
        if (spacing == 0) return;

        var text = tb.Text;
        if (string.IsNullOrEmpty(text)) return;

        tb.Inlines.Clear();
        for (var i = 0; i < text.Length; i++)
        {
            tb.Inlines.Add(new Run(text[i].ToString()));
            // Add a thin space after every character except the last
            if (i < text.Length - 1)
                tb.Inlines.Add(new Run(" ")
                {
                    FontSize    = tb.FontSize * (spacing / 10.0),
                    BaselineAlignment = BaselineAlignment.Subscript,
                });
        }
    }
}
