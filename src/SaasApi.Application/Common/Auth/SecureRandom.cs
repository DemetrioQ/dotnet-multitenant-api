using System.Security.Cryptography;

namespace SaasApi.Application.Common.Auth;

public static class SecureRandom
{
    /// <summary>URL-safe base64 string of <paramref name="byteCount"/> random bytes (no padding).</summary>
    public static string UrlSafeToken(int byteCount)
    {
        var bytes = RandomNumberGenerator.GetBytes(byteCount);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}
