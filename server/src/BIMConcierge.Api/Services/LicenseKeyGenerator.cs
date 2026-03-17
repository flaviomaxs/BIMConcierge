using System.Security.Cryptography;

namespace BIMConcierge.Api.Services;

public static class LicenseKeyGenerator
{
    private static readonly char[] Chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789".ToCharArray();

    /// <summary>
    /// Generates a license key in the format BIM-XXXX-XXXX-XXXX.
    /// </summary>
    public static string Generate()
    {
        return $"BIM-{Block()}-{Block()}-{Block()}";

        static string Block()
        {
            Span<char> buf = stackalloc char[4];
            for (int i = 0; i < 4; i++)
                buf[i] = Chars[RandomNumberGenerator.GetInt32(Chars.Length)];
            return new string(buf);
        }
    }
}
