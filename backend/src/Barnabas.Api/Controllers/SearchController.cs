using Barnabas.Application.Search.SearchListings;
using Barnabas.Domain.Listings;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Barnabas.Api.Controllers;

/// <summary>Finding something on the board.</summary>
/// <remarks>
/// The page size is clamped here rather than validated, for the same reason the board's is: it is
/// a transport hint rather than something a member typed, and refusing a caller who asked for too
/// much would give them nothing at all.
/// </remarks>
[ApiController]
[Route("search")]
public sealed class SearchController : ControllerBase
{
    private const int DefaultLimit = 24;
    private const int MaximumLimit = 50;

    private readonly ISender _sender;

    public SearchController(ISender sender) => _sender = sender;

    [HttpGet]
    [ProducesResponseType<SearchPage>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SearchPage>> Search(
        [FromQuery] string term,
        [FromQuery] ListingKind? kind,
        [FromQuery] string? neighbourhood,
        [FromQuery] decimal? maxPrice,
        [FromQuery] int? limit,
        [FromQuery] string? cursor,
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(
            new SearchListingsQuery(
                term ?? string.Empty,
                kind,
                neighbourhood,
                maxPrice,
                Math.Clamp(limit ?? DefaultLimit, 1, MaximumLimit),
                cursor),
            cancellationToken));
}
