using Barnabas.Api.Contracts;
using Barnabas.Application.Joining.IssueInviteCode;
using Barnabas.Application.Joining.RedeemInviteCode;
using Barnabas.Application.Joining.RevokeInviteCode;
using Barnabas.Application.Joining.SubmitJoiningProfile;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Barnabas.Api.Controllers;

/// <summary>
/// Issuing a code, and joining with one.
/// </summary>
/// <remarks>
/// The two halves face opposite ways. Issuing and revoking demand a moderator and take the
/// congregation from the verified session. Redeeming and joining are anonymous — nobody is signed
/// in, and the code is what reveals which congregation is being joined — so they say
/// <c>AllowAnonymous</c> against the fallback policy that closes everything else.
/// <para>
/// An unknown code answers 404 and a dead one 410, with the same body: expired, spent and revoked
/// are one reply, because saying which would disclose something about a code the caller is not
/// entitled to know about.
/// </para>
/// </remarks>
[ApiController]
public sealed class JoiningController : ControllerBase
{
    private readonly ISender _sender;

    public JoiningController(ISender sender) => _sender = sender;

    [HttpPost("invites")]
    [ProducesResponseType<IssuedInviteCodeResult>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IssuedInviteCodeResult>> Issue(CancellationToken cancellationToken)
    {
        var issued = await _sender.Send(new IssueInviteCodeCommand(), cancellationToken);

        return Created($"/invites/{issued.InviteCodeId}", issued);
    }

    [HttpDelete("invites/{inviteCodeId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Revoke(Guid inviteCodeId, CancellationToken cancellationToken)
    {
        await _sender.Send(new RevokeInviteCodeCommand(inviteCodeId), cancellationToken);

        return NoContent();
    }

    [AllowAnonymous]
    [HttpPost("invites/redeem")]
    [ProducesResponseType<RedeemedInviteCodeResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status410Gone)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<RedeemedInviteCodeResult>> Redeem(
        RedeemInviteCodeRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        return Ok(await _sender.Send(new RedeemInviteCodeCommand(request.Code), cancellationToken));
    }

    [AllowAnonymous]
    [HttpPost("joining/profile")]
    [ProducesResponseType<JoinedCongregationResult>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status410Gone)]
    public async Task<ActionResult<JoinedCongregationResult>> SubmitProfile(
        SubmitJoiningProfileRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var joined = await _sender.Send(
            new SubmitJoiningProfileCommand(
                request.JoiningToken,
                request.EmailAddress,
                request.DisplayName,
                request.Neighbourhood,
                request.ReasonForJoining),
            cancellationToken);

        return Created($"/members/{joined.MemberId}", joined);
    }
}
