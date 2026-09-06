using Barnabas.Infrastructure.Email;
using Barnabas.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Barnabas.Api.Controllers;

/// <summary>
/// Reads back what the outbox holds, so an automated browser can follow a real sign-in link.
/// </summary>
/// <remarks>
/// Sign-in is passwordless, so the Playwright suite has no way to complete a sign-in without
/// reading the link out of the mail that was never actually posted. The alternative was a
/// test-only endpoint that mints a session, which would leave the real sign-in screens
/// untested - and those carry acceptance criteria of their own.
/// <para>
/// This controller is registered only when the environment is Development. It is not
/// conditional on a flag that could be set in production by mistake: the registration itself is
/// absent, so the route does not exist to be found.
/// </para>
/// </remarks>
[ApiController]
[AllowAnonymous]
[Route("dev")]
public sealed class DevelopmentController : ControllerBase
{
    private readonly IEmailOutbox _outbox;
    private readonly DatabaseReset _reset;

    public DevelopmentController(IEmailOutbox outbox, DatabaseReset reset)
    {
        _outbox = outbox;
        _reset = reset;
    }

    [HttpGet("sign-in-links/{emailAddress}")]
    [ProducesResponseType<SignInLinkMessage>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<SignInLinkMessage> Latest(string emailAddress)
    {
        var message = _outbox.LatestFor(emailAddress);

        return message is null ? NotFound() : Ok(message);
    }

    /// <summary>
    /// Puts the board back to the seeded congregation.
    /// </summary>
    /// <remarks>
    /// The Playwright suite calls this before each spec. Without it the specs would depend on
    /// the order they ran in, because each one posts listings and makes requests that the next
    /// would find waiting.
    /// </remarks>
    [HttpPost("reset")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Reset(CancellationToken cancellationToken)
    {
        await _reset.ResetAsync(cancellationToken);

        _outbox.Clear();

        return NoContent();
    }
}
