using System.Security.Claims;
using Barnabas.Application.Common.Tenancy;
using Barnabas.Domain.Members;
using Barnabas.Infrastructure.Security;

namespace Barnabas.Api.Tenancy;

/// <summary>
/// Reads the caller's congregation, member, session, and role from the signed token.
/// </summary>
/// <remarks>
/// Every value comes from a claim, so a caller cannot alter one without invalidating the
/// signature. Nothing here reads a route value, a query string, or a body: those are the
/// places a caller could name somebody else.
/// </remarks>
public sealed class CongregationContext : ICongregationContext
{
    private readonly Lazy<Identity?> _identity;

    public CongregationContext(IHttpContextAccessor accessor)
    {
        ArgumentNullException.ThrowIfNull(accessor);

        _identity = new Lazy<Identity?>(() => Read(accessor.HttpContext?.User));
    }

    public bool IsResolved => _identity.Value is not null;

    public Guid CongregationId => Require().CongregationId;

    public Guid MemberId => Require().MemberId;

    public Guid SessionId => Require().SessionId;

    public MemberRole Role => Require().Role;

    private static Identity? Read(ClaimsPrincipal? principal)
    {
        if (principal?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        if (!Guid.TryParse(principal.FindFirstValue(BarnabasClaims.MemberId), out var memberId)
            || !Guid.TryParse(principal.FindFirstValue(BarnabasClaims.CongregationId), out var congregationId)
            || !Guid.TryParse(principal.FindFirstValue(BarnabasClaims.SessionId), out var sessionId)
            || !Enum.TryParse<MemberRole>(principal.FindFirstValue(BarnabasClaims.Role), out var role))
        {
            return null;
        }

        return new Identity(congregationId, memberId, sessionId, role);
    }

    private Identity Require() =>
        _identity.Value ?? throw new InvalidOperationException(
            "No congregation is in scope. Check IsResolved before reading the caller's identity.");

    private sealed record Identity(Guid CongregationId, Guid MemberId, Guid SessionId, MemberRole Role);
}
