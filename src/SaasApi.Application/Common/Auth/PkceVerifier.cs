using System.Security.Cryptography;
using System.Text;

namespace SaasApi.Application.Common.Auth;

/// <summary>
/// PKCE verification per RFC 7636 §4.6.
/// Only the S256 challenge method is supported (the "plain" method is
/// deprecated for security reasons — never accept it).
/// </summary>
public static class PkceVerifier
{
    public const string S256 = "S256";

    /// <summary>
    /// Verifies that <paramref name="codeVerifier"/> hashes to <paramref name="storedChallenge"/>
    /// under the requested method. Returns false on any mismatch, unsupported method,
    /// or null/empty input.
    /// </summary>
    public static bool Verify(string? codeVerifier, string? storedChallenge, string? challengeMethod)
    {
        if (string.IsNullOrEmpty(codeVerifier) || string.IsNullOrEmpty(storedChallenge))
            return false;

        if (!string.Equals(challengeMethod, S256, StringComparison.Ordinal))
            return false;

        var hashed = ComputeS256(codeVerifier);
        // Constant-time comparison to avoid timing-based oracle attacks.
        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(hashed),
            Encoding.ASCII.GetBytes(storedChallenge));
    }

    /// <summary>
    /// Compute S256 challenge: BASE64URL(SHA256(verifier)) — used by clients
    /// when constructing the authorization URL. Exposed for tests and shared logic.
    /// </summary>
    public static string ComputeS256(string codeVerifier)
    {
        var hash = SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier));
        return Base64Url(hash);
    }

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
}
