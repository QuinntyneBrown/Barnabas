using System.Security.Claims;
using Barnabas.Application.Common.Persistence;
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
        }
    }
}
