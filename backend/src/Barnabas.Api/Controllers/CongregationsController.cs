using Barnabas.Api.Contracts;
using Barnabas.Application.Congregations.ConfigureCongregation;
using Barnabas.Application.Congregations.DesignateModerator;
using Barnabas.Application.Congregations.GetCongregation;
using Barnabas.Application.Congregations.ProvisionCongregation;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Barnabas.Api.Controllers;

/// <summary>
/// Bringing a congregation into being, and configuring it.
/// </summary>
/// <remarks>
/// Every route but one demands <c>Administrator</c>, and the role is a property of the caller
/// rather than of a resource — so refusing it discloses nothing and answers 403. Naming a
/// congregation that is not there answers 404, because identity is a property of the boundary and
/// must not confirm what exists.
/// <para>
/// <c>GET /congregation</c> is the exception: it names no congregation at all, taking the
/// identifier from the verified session, so any member may ask it about their own.
/// </para>
/// </remarks>
[ApiController]
public sealed class CongregationsController : ControllerBase
{
    private readonly ISender _sender;

    public CongregationsController(ISender sender) => _sender = sender;

    [HttpPost("congregations")]
    [ProducesResponseType<ProvisionedCongregationResult>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProvisionedCongregationResult>> Provision(
        ProvisionCongregationCommand command,
        CancellationToken cancellationToken)
    {
        var provisioned = await _sender.Send(command, cancellationToken);

        return Created($"/congregations/{provisioned.CongregationId}", provisioned);
    }

    [HttpPut("congregations/{congregationId:guid}")]
    [ProducesResponseType<ConfiguredCongregationResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ConfiguredCongregationResult>> Configure(
        Guid congregationId,
        ConfigureCongregationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        return Ok(await _sender.Send(
            new ConfigureCongregationCommand(congregationId, request.Name, request.Neighbourhoods),
            cancellationToken));
    }

    [HttpPost("congregations/{congregationId:guid}/moderators/{memberId:guid}")]
    [ProducesResponseType<DesignatedModeratorResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DesignatedModeratorResult>> DesignateModerator(
        Guid congregationId,
        Guid memberId,
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new DesignateModeratorCommand(congregationId, memberId), cancellationToken));

    /// <summary>The caller's own congregation, so no screen has to hard-code a parish.</summary>
    [HttpGet("congregation")]
    [ProducesResponseType<CongregationDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CongregationDto>> Current(CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new GetCongregationQuery(), cancellationToken));
}
