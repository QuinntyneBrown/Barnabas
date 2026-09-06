using Barnabas.Application.Common.Authorisation;
using Barnabas.Domain.Listings;
using MediatR;

namespace Barnabas.Application.Photos.AttachPhoto;

/// <summary>
/// Puts a photo on a listing.
/// </summary>
/// <remarks>
/// The owner's, and nobody else's — the ownership marker is what says so, and it is the same one
/// editing and closing out declare. A moderator has no business adding a picture to somebody's
/// listing, so this is the one moderation-adjacent thing they cannot do.
/// </remarks>
public sealed record AttachPhotoCommand(Guid ListingId, byte[] Bytes, string ContentType)
    : IRequest<AttachedPhotoResult>, IRequireOwnership<Listing>
{
    public Guid ResourceId => ListingId;
}
