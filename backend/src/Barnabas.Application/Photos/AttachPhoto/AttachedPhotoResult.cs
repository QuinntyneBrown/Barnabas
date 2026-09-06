namespace Barnabas.Application.Photos.AttachPhoto;

/// <summary>The photo now on the listing, and where to fetch it from.</summary>
public sealed record AttachedPhotoResult(Guid ListingId, Guid PhotoId, string Url, string BoardUrl);
