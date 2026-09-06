using Barnabas.Api.Contracts;
using Barnabas.Api.Security;
using Barnabas.Application.Access;
using Barnabas.Application.Access.ExchangeSignInToken;
using Barnabas.Application.Access.RefreshSession;
using Barnabas.Application.Access.RequestSignInLink;
using Barnabas.Application.Access.SignOut;
using Barnabas.Domain.Access;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Barnabas.Api.Controllers;

/// <summary>
/// Signing in, renewing, and signing out.
/// </summary>
/// <remarks>
/// The first three actions are among the few endpoints exempt from authentication, for the
/// obvious reason: a member asking for a sign-in link has no token, and one whose access token
/// has expired cannot present a valid one to ask for a new one.
/// </remarks>
[ApiController]
[Route("sessions")]
public sealed class SessionsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly RefreshCookieOptions _cookie;

    public SessionsController(ISender sender, IOptions<RefreshCookieOptions> cookie)
    {
        ArgumentNullException.ThrowIfNull(cookie);

        _sender = sender;
        _cookie = cookie.Value;
    }

    /// <summary>
    /// Asks for a sign-in link.
    /// </summary>
    /// <remarks>
    /// Always 202, registered address or not. The endpoint cannot be used to discover who is in
    /// the congregation, and the empty body is what keeps the two cases identical.
    /// </remarks>
    [AllowAnonymous]
    [HttpPost("link")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RequestLink(RequestSignInLinkRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await _sender.Send(new RequestSignInLinkCommand(request.EmailAddress), cancellationToken);

        return Accepted();
    }

    [AllowAnonymous]
    [HttpPost]
    [ProducesResponseType<SessionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status410Gone)]
    public async Task<ActionResult<SessionResponse>> Exchange(
        ExchangeSignInTokenRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var session = await _sender.Send(new ExchangeSignInTokenCommand(request.Token), cancellationToken);

        return Issue(session);
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    [ProducesResponseType<SessionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SessionResponse>> Refresh(CancellationToken cancellationToken)
    {
        // No body to read. A refresh token in a payload would be a refresh token that script on
        // the page had to be able to reach.
        if (!Request.Cookies.TryGetValue(RefreshCookieOptions.CookieName, out var refreshToken))
        {
            throw new RefreshTokenNotActiveException();
        }

        var session = await _sender.Send(new RefreshSessionCommand(refreshToken), cancellationToken);

        return Issue(session);
    }

    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SignOut(CancellationToken cancellationToken)
    {
        await _sender.Send(new SignOutCommand(), cancellationToken);

        Response.Cookies.Delete(RefreshCookieOptions.CookieName, CookieSettings(expired: true));

        return NoContent();
    }

    private ActionResult<SessionResponse> Issue(SessionResult session)
    {
        Response.Cookies.Append(RefreshCookieOptions.CookieName, session.RefreshToken, CookieSettings(expired: false));

        return Ok(new SessionResponse(
            session.SessionId,
            session.AccessToken,
            session.ExpiresOn,
            session.Status.ToString(),
            session.Role.ToString()));
    }

    private CookieOptions CookieSettings(bool expired) => new()
    {
        HttpOnly = true,
        Secure = _cookie.Secure,
        SameSite = SameSiteMode.Strict,

        // The root rather than /sessions: the browser sees the web client's proxy prefix, and a
        // narrower path would simply never be sent back.
        Path = "/",
        Expires = expired ? DateTimeOffset.UnixEpoch : DateTimeOffset.UtcNow.Add(RefreshToken.Lifetime),
    };
}
