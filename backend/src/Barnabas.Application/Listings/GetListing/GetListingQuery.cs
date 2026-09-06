using MediatR;

namespace Barnabas.Application.Listings.GetListing;

/// <summary>Asks for one listing of the caller's congregation.</summary>
public sealed record GetListingQuery(Guid ListingId) : IRequest<ListingDetailDto>;
