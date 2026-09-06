namespace Barnabas.Infrastructure.Security;

/// <summary>
/// The claims an access token carries, named in one place so the issuer and the reader cannot
/// drift apart.
/// </summary>
public static class BarnabasClaims
{
    public const string MemberId = "sub";

    public const string CongregationId = "congregation";

    public const string Role = "role";

    public const string SessionId = "sid";
}
