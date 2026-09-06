using Barnabas.Application.Common.Exceptions;
using Barnabas.Application.Common.Persistence;
using Barnabas.Domain.Listings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Barnabas.Application.Listings.EditListing;

/// <summary>Applies the correction and commits.</summary>
/// <remarks>
/// Ownership has already been enforced by the pipeline, so a handler that is reached is one the
/// caller was entitled to reach. The read still goes through the filtered set, so a listing in
/// another congregation is absent rather than forbidden.
/// </remarks>
public sealed class EditListingCommandHandler : IRequestHandler<EditListingCommand, EditedListingResult>
{
    private readonly IBarnabasDbContext _context;

    public EditListingCommandHandler(IBarnabasDbContext context) => _context = context;

    public async Task<EditedListingResult> Handle(EditListingCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var listing = await _context.Listings
            .FirstOrDefaultAsync(listing => listing.Id == request.ListingId, cancellationToken)
            ?? throw new NotFoundException();

        listing.Edit(request.Title, request.Description, request.Category, request.Neighbourhood);

        await _context.SaveChangesAsync(cancellationToken);

        return new EditedListingResult(listing.Id, listing.Title);
    }
}
