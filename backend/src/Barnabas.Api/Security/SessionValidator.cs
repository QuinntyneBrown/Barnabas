using System.Security.Claims;
using Barnabas.Application.Common.Persistence;
using Barnabas.Domain.Members;
using Barnabas.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Barnabas.Api.Security;

/// <summary>
/// Refuses a token whose session has ended, however well signed it is.
/// </summary>
/// <remarks>
/// Authentication has two halves. A signature proves the token was issued by this system and
/// has not been altered; it cannot prove the session behind it still stands. Revocation is
/// state, so it is read as state - once per authenticated request, by primary key.
/// <para>
/// That read is the price of <c>L2-019</c>, which requires a signed-out member's tokens to stop
/// being accepted and admits no window. A single keyed read sits comfortably inside the budget
/// <c>L2-103</c> allows at the scale of a congregation.
/// </para>
/// <para>
/// The member is read alongside it, and their role and status are stamped onto this request's
/// principal. <c>L2-003</c> asks that a moderator grant work for that member's <em>subsequent</em>
/// requests, and <c>L2-086</c> that an approval let them onto the board on their next visit -
/// neither of which a claim minted an hour ago can promise.
/// </para>
/// </remarks>
public sealed class SessionValidator
{
    public static async Task ValidateAsync(TokenValidatedContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var sessionId = context.Principal?.FindFirstValue(BarnabasClaims.SessionId);

        // A token naming no session cannot be checked for revocation, so it is refused rather
        // than trusted. L2-014 requires exactly that.
        if (!Guid.TryParse(sessionId, out var id))
        {
            context.Fail("The token carries no session identifier.");

            return;
        }

        var store = context.HttpContext.RequestServices.GetRequiredService<IAuthenticationStore>();
        var time = context.HttpContext.RequestServices.GetRequiredService<TimeProvider>();

        var session = await store.FindSessionAsync(id, context.HttpContext.RequestAborted);

        if (session is null || !session.IsLive(time.GetUtcNow()))
        {
            context.Fail("The session has ended.");

            return;
        }

        var member = await store.FindMemberByIdAsync(session.MemberId, context.HttpContext.RequestAborted);

        if (member is null)
        {
            context.Fail("The session has ended.");

            return;
        }

        Restamp(context.Principal, member);
    }

    /// <summary>
    /// Replaces the token's role with the record's, and adds the status it never carried.
    /// </summary>
    /// <remarks>
    /// Replaced rather than appended, because two role claims would let whichever is found first
    /// win - and which that is, is not something to leave to ordering.
    /// </remarks>
    private static void Restamp(ClaimsPrincipal? principal, Member member)
    {
        if (principal?.Identity is not ClaimsIdentity identity)
        {
            return;
        }

        foreach (var stale in identity.FindAll(BarnabasClaims.Role).ToList())
        {
            identity.RemoveClaim(stale);
        }

        foreach (var stale in identity.FindAll(BarnabasClaims.Status).ToList())
        {
            identity.RemoveClaim(stale);
        }

        identity.AddClaim(new Claim(BarnabasClaims.Role, member.Role.ToString()));
        identity.AddClaim(new Claim(BarnabasClaims.Status, member.Status.ToString()));
    }
}
