using BIMConcierge.Core.Interfaces;
using Serilog;
using System.IO;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;

namespace BIMConcierge.Infrastructure.Auth;

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
        set { _accessToken = value; SaveAsync("access.dat", value); }
    }

    public string? RefreshToken
    {
        get => _refreshToken ??= Load("refresh.dat");
        set { _refreshToken = value; SaveAsync("refresh.dat", value); }
    }

    /// <summary>
    /// Persists the token to disk asynchronously via fire-and-forget to avoid blocking the UI thread.
    /// </summary>
    private static async void SaveAsync(string file, string? value)
    {
        try
        {
            var path = Path.Combine(_tokenDir, file);
            if (value is null)
            {
                if (File.Exists(path)) File.Delete(path);
                return;
            }
            var data = Encoding.UTF8.GetBytes(value);
            var encrypted = ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
            await File.WriteAllBytesAsync(path, encrypted).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to persist token file {File}", file);
        }
    }

    private static string? Load(string file)
    {
        var path = Path.Combine(_tokenDir, file);
        if (!File.Exists(path)) return null;
        try
        {
            var encrypted = File.ReadAllBytes(path);
            var data = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(data);
        }
        catch (CryptographicException ex)
        {
            Log.Warning(ex, "Failed to decrypt token file {File} — it may be corrupted or from a different user", file);
            return null;
        }
        catch (IOException ex)
        {
            Log.Warning(ex, "Failed to read token file {File}", file);
            return null;
        }
    }

    private static void EnsureDir() => Directory.CreateDirectory(_tokenDir);
}
