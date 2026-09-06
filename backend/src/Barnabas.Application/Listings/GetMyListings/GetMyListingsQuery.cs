using MediatR;

namespace Barnabas.Application.Listings.GetMyListings;

/// <summary>
/// Asks for the caller's own listings.
/// </summary>
/// <remarks>
/// The caller is not a parameter. It comes from the session, so there is no identifier to change
/// in order to read somebody else's.
/// </remarks>
public sealed record GetMyListingsQuery(bool IncludeClosed) : IRequest<IReadOnlyList<MyListingDto>>;
