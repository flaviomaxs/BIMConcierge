namespace BIMConcierge.Core.Interfaces;

/// <summary>
/// Abstracts window navigation so ViewModels can open windows without
/// depending on the UI layer or IServiceProvider directly.
/// </summary>
public interface INavigationService
{
    void NavigateTo(string windowName);
}
