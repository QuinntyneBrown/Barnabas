using System.Text;
using System.Text.Json;

namespace Barnabas.IntegrationTests.Fixtures;

/// <summary>
/// Rewrites a claim inside a genuine token without re-signing it.
/// </summary>
/// <remarks>
/// This is the attack L2-088 AC3 describes: not a forged token, but a real one whose congregation
/// has been edited by whoever was holding it. The signature is left as it was, which is precisely
/// why it must stop validating.
/// </remarks>
public static class TamperedTokens
{
    public static string WithCongregation(string token, Guid congregationId)
    {
        ArgumentNullException.ThrowIfNull(token);

        var parts = token.Split('.');

        var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(Decode(parts[1]))!;

        payload["congregation"] = JsonSerializer.SerializeToElement(congregationId.ToString());

        parts[1] = Encode(JsonSerializer.SerializeToUtf8Bytes(payload));

        return string.Join('.', parts);
    }

    private static byte[] Decode(string segment)
    {
        var padded = segment.Replace('-', '+').Replace('_', '/');

        return Convert.FromBase64String(padded.PadRight(padded.Length + ((4 - (padded.Length % 4)) % 4), '='));
    }

    private static string Encode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
