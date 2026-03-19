namespace BIMConcierge.Core.Interfaces;

public interface IStringLocalizer
{
    string GetString(string key);
    string Format(string key, params object[] args);
}
