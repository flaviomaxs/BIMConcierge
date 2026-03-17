namespace BIMConcierge.Core.Interfaces;

public interface ITokenStore
{
    string? AccessToken { get; set; }
    string? RefreshToken { get; set; }
}
