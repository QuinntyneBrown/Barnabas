using System.Security.Cryptography;
using System.Text;
using Barnabas.Application.Common.Security;

namespace Barnabas.Infrastructure.Security;

/// <inheritdoc />
public sealed class SecretService : ISecretService
{
    private const int SecretBytes = 32;

    public string CreateSecret() => Base64UrlEncode(RandomNumberGenerator.GetBytes(SecretBytes));

    /// <remarks>
    /// A plain SHA-256 rather than a password hash. The input is 256 bits of cryptographic
    /// randomness, not something a member chose, so there is no dictionary to run against it
    /// and nothing for a work factor to buy.
    /// </remarks>
    public string Hash(string secret)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secret);

        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(secret)));
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
