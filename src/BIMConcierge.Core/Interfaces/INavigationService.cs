namespace BIMConcierge.Core.Interfaces;

/// <summary>
/// Abstracts section navigation inside the single DashboardWindow.
/// ViewModels use this to switch the active section without depending on UI types directly.
/// </summary>
public interface INavigationService
{
    void NavigateTo(string section);
    void NavigateTo(string section, object? parameter);
}
