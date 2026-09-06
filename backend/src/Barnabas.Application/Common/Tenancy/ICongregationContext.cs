namespace Barnabas.Application.Common.Tenancy;

/// <summary>
/// The congregation and member the current token names.
/// </summary>
/// <remarks>
/// Every value here comes from a signed claim, so a caller cannot alter one without
/// invalidating the signature. The persistence layer depends on this abstraction and on
/// nothing about HTTP.
/// </remarks>
public interface ICongregationContext
{
    /// <summary>
    /// Whether a congregation is in scope. Signing in is anonymous, so this is false for
    /// the sign-in endpoints and the database context fails closed rather than serving
    /// unfiltered rows.
    /// </summary>
    bool IsResolved { get; }

    Guid CongregationId { get; }

    Guid MemberId { get; }

    Guid SessionId { get; }

    Barnabas.Domain.Members.MemberRole Role { get; }

    /// <summary>
    /// Whether the caller has been let into their congregation.
    /// </summary>
    /// <remarks>
    /// Read from the member record on every request rather than from the token, so an approval
    /// takes effect on the next visit rather than the next sign-in.
    /// </remarks>
    Barnabas.Domain.Members.MemberStatus Status { get; }
}
