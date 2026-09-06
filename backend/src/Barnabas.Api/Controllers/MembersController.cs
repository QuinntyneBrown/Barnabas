using Barnabas.Api.Contracts;
using Barnabas.Application.Members.EditMyProfile;
using Barnabas.Application.Members.EraseMyData;
using Barnabas.Application.Members.ExportMyData;
using Barnabas.Application.Members.GetDirectory;
using Barnabas.Application.Members.GetMemberProfile;
using Barnabas.Application.Members.GetMyProfile;
using Barnabas.Application.Members.LeaveCongregation;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Barnabas.Api.Controllers;

/// <summary>
/// A member's own profile, another member's, and the congregation's list of them.
/// </summary>
/// <remarks>
/// <c>me</c> is routed before <c>{memberId}</c> so the literal segment wins; nothing else would
/// stop "me" being read as a malformed identifier.
/// <para>
/// There is no endpoint here that starts a conversation. A request against a listing, once
/// accepted, is the only thing that opens a thread — <c>L2-069</c>.
/// </para>
/// </remarks>
[ApiController]
public sealed class MembersController : ControllerBase
{
    private readonly ISender _sender;

    public MembersController(ISender sender) => _sender = sender;

    [HttpGet("members/me")]
    [ProducesResponseType<MyProfileDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<MyProfileDto>> Me(CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new GetMyProfileQuery(), cancellationToken));

    [HttpPut("members/me")]
    [ProducesResponseType<MyProfileDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MyProfileDto>> EditMe(
        EditMyProfileRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        return Ok(await _sender.Send(
            new EditMyProfileCommand(
                request.DisplayName,
                request.Neighbourhood,
                request.Description,
                request.HelpTags),
            cancellationToken));
    }

    /// <summary>Leaves the congregation, withdrawing their listings and ending their session.</summary>
    [HttpPost("members/me/leave")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> Leave(CancellationToken cancellationToken)
    {
        await _sender.Send(new LeaveCongregationCommand(), cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Everything Barnabas holds about the caller, in one document. L2-101 AC3.
    /// </summary>
    /// <remarks>
    /// It names no member. The one exported comes from the verified session, so there is no
    /// identifier a caller could change in order to export somebody else.
    /// </remarks>
    [HttpGet("members/me/export")]
    [ProducesResponseType<MyDataExport>(StatusCodes.Status200OK)]
    public async Task<ActionResult<MyDataExport>> Export(CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new ExportMyDataQuery(), cancellationToken));

    /// <summary>
    /// The caller asks to be forgotten. L2-101 AC2.
    /// </summary>
    /// <remarks>
    /// A separate act from leaving, and it implies leaving. Their profile, listings and messages
    /// are irreversibly anonymised in place — deleting them would break the other party's record
    /// of a conversation they are entitled to keep — and every session they hold ends.
    /// </remarks>
    [HttpPost("members/me/erase")]
    [ProducesResponseType<ErasedDataResult>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ErasedDataResult>> Erase(CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new EraseMyDataCommand(), cancellationToken));

    [HttpGet("members/{memberId:guid}")]
    [ProducesResponseType<MemberProfileDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MemberProfileDto>> Profile(
        Guid memberId,
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new GetMemberProfileQuery(memberId), cancellationToken));

    [HttpGet("directory")]
    [ProducesResponseType<IReadOnlyList<DirectoryMemberDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DirectoryMemberDto>>> Directory(
        [FromQuery] string? term,
        [FromQuery] string? helpTag,
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new GetDirectoryQuery(term, helpTag), cancellationToken));
}
