using BIMConcierge.Core.Interfaces;

namespace BIMConcierge.UI.Localization;

public class AppStringLocalizer : IStringLocalizer
{
    public string GetString(string key) => TranslationSource.GetString(key);

    public string Format(string key, params object[] args) => TranslationSource.Format(key, args);
}
