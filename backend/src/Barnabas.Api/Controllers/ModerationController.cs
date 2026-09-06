using Barnabas.Api.Contracts;
using Barnabas.Application.Moderation.ApproveListing;
using Barnabas.Application.Moderation.ApproveMember;
using Barnabas.Application.Moderation.DeclineMember;
using Barnabas.Application.Moderation.GetModerationQueue;
using Barnabas.Application.Moderation.GetPendingMembers;
using Barnabas.Application.Moderation.RemoveListing;
using Barnabas.Application.Moderation.ReportListing;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Barnabas.Api.Controllers;

/// <summary>
/// Reporting a listing, and the two queues a moderator works through.
/// </summary>
/// <remarks>
/// The two halves face opposite ways. Reporting is something any member does about somebody else's
/// listing; everything under <c>/moderation</c> demands the moderator role, declared on the
/// command rather than checked here, so a route added later cannot forget it.
/// <para>
/// No route names a congregation. The one being moderated comes from the verified session, which
/// is why a listing in another parish answers 404 rather than 403 — <c>L2-084 AC2</c>.
/// </para>
/// </remarks>
[ApiController]
public sealed class ModerationController : ControllerBase
{
    private readonly ISender _sender;

    public ModerationController(ISender sender) => _sender = sender;

    /// <summary>Any member may report any listing they can see.</summary>
    [HttpPost("listings/{listingId:guid}/reports")]
    [ProducesResponseType<ReportedListingResult>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReportedListingResult>> Report(
        Guid listingId,
        ReportListingRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var reported = await _sender.Send(
            new ReportListingCommand(listingId, request.Reason, request.Note),
            cancellationToken);

        return Created($"/listings/{listingId}/reports/{reported.ReportId}", reported);
    }

    [HttpGet("moderation/listings")]
    [ProducesResponseType<IReadOnlyList<FlaggedListingDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<FlaggedListingDto>>> Queue(
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new GetModerationQueueQuery(), cancellationToken));

    [HttpPost("moderation/listings/{listingId:guid}/approve")]
    [ProducesResponseType<ReviewedListingResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReviewedListingResult>> ApproveListing(
        Guid listingId,
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new ApproveListingCommand(listingId), cancellationToken));

    [HttpPost("moderation/listings/{listingId:guid}/remove")]
    [ProducesResponseType<ReviewedListingResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReviewedListingResult>> RemoveListing(
        Guid listingId,
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new RemoveListingCommand(listingId), cancellationToken));

    [HttpGet("moderation/members")]
    [ProducesResponseType<IReadOnlyList<PendingMemberDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<PendingMemberDto>>> Pending(
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new GetPendingMembersQuery(), cancellationToken));

    [HttpPost("moderation/members/{memberId:guid}/approve")]
    [ProducesResponseType<DecidedMemberResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DecidedMemberResult>> ApproveMember(
        Guid memberId,
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new ApproveMemberCommand(memberId), cancellationToken));

    [HttpPost("moderation/members/{memberId:guid}/decline")]
    [ProducesResponseType<DecidedMemberResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DecidedMemberResult>> DeclineMember(
        Guid memberId,
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new DeclineMemberCommand(memberId), cancellationToken));
}
