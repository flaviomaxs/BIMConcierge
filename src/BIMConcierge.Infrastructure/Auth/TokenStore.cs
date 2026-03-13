using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;

namespace BIMConcierge.Infrastructure.Auth;

public interface ITokenStore
{
    string? AccessToken  { get; set; }
    string? RefreshToken { get; set; }
}

/// <summary>
/// Persists JWT tokens encrypted via Windows DPAPI (CurrentUser scope).
/// Falls back to in-memory if DPAPI is unavailable.
/// </summary>
[SupportedOSPlatform("windows")]
public class TokenStore : ITokenStore
{
    private static readonly string _tokenDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "BIMConcierge", "auth");

    private string? _accessToken;
    private string? _refreshToken;

    public TokenStore() => EnsureDir();

    public string? AccessToken
    {
        get => _accessToken ??= Load("access.dat");
        set { _accessToken = value; Save("access.dat", value); }
    }

    public string? RefreshToken
    {
        get => _refreshToken ??= Load("refresh.dat");
        set { _refreshToken = value; Save("refresh.dat", value); }
    }

    private static void Save(string file, string? value)
    {
        var path = Path.Combine(_tokenDir, file);
        if (value is null) { if (File.Exists(path)) File.Delete(path); return; }
        var data      = Encoding.UTF8.GetBytes(value);
        var encrypted = ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(path, encrypted);
    }

    private static string? Load(string file)
    {
        var path = Path.Combine(_tokenDir, file);
        if (!File.Exists(path)) return null;
        try
        {
            var encrypted = File.ReadAllBytes(path);
            var data      = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(data);
        }
        catch { return null; }
    }

    private static void EnsureDir() => Directory.CreateDirectory(_tokenDir);
}
