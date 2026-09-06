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

    /// <summary>
    /// Whether the member has been let in yet.
    /// </summary>
    /// <remarks>
    /// Stamped onto the principal from the member record on every request rather than minted into
    /// the token, so a moderator's approval takes effect on the next visit instead of on the next
    /// sign-in. The same is true of the role: L2-003 asks that a grant work for the member's
    /// <em>subsequent</em> requests, which a claim baked in an hour ago cannot promise.
    /// </remarks>
    public const string Status = "status";
}
