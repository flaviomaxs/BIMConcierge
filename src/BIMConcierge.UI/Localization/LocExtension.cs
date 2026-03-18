using System.Windows.Data;
using System.Windows.Markup;

namespace BIMConcierge.UI.Localization;

/// <summary>
/// XAML markup extension for localized strings.
/// Usage: <c>Text="{loc:Loc LoginTitle}"</c>
/// Automatically updates when the language changes.
/// </summary>
[MarkupExtensionReturnType(typeof(string))]
public sealed class LocExtension : MarkupExtension
{
    public string Key { get; }

    public LocExtension(string key) => Key = key;

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var binding = new Binding($"[{Key}]")
        {
            Source = TranslationSource.Instance,
            Mode = BindingMode.OneWay
        };

        return binding.ProvideValue(serviceProvider);
    }
}
