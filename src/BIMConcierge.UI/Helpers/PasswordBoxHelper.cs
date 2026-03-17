using System.Windows;
using System.Windows.Controls;

namespace BIMConcierge.UI.Helpers;

/// <summary>
/// Attached property that enables data binding on WPF PasswordBox.Password.
/// Usage: helpers:PasswordBoxHelper.BoundPassword="{Binding Password, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"
/// </summary>
public static class PasswordBoxHelper
{
    public static readonly DependencyProperty BoundPasswordProperty =
        DependencyProperty.RegisterAttached(
            "BoundPassword",
            typeof(string),
            typeof(PasswordBoxHelper),
            new FrameworkPropertyMetadata(string.Empty, OnBoundPasswordChanged));

    private static bool _updating;

    public static string GetBoundPassword(DependencyObject d) =>
        (string)d.GetValue(BoundPasswordProperty);

    public static void SetBoundPassword(DependencyObject d, string value) =>
        d.SetValue(BoundPasswordProperty, value);

    private static void OnBoundPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not PasswordBox pb) return;

        pb.PasswordChanged -= OnPasswordChanged;

        if (!_updating)
            pb.Password = (string)e.NewValue;

        pb.PasswordChanged += OnPasswordChanged;
    }

    private static void OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is not PasswordBox pb) return;

        _updating = true;
        SetBoundPassword(pb, pb.Password);
        _updating = false;
    }
}
