namespace BIMConcierge.UI;

/// <summary>
/// Static service locator set by the Plugin layer at startup.
/// Allows UI views to resolve ViewModels without a circular reference to BIMConcierge.Plugin.
/// </summary>
public static class ServiceLocator
{
    public static IServiceProvider? ServiceProvider { get; set; }
}
