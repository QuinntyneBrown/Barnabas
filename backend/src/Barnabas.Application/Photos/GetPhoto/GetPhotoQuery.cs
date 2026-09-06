using Barnabas.Application.Common.Authorisation;
using Barnabas.Application.Photos.Common;
using Barnabas.Domain.Photos;
using MediatR;

namespace Barnabas.Application.Photos.GetPhoto;

/// <summary>
/// The bytes of one rendition of one photo.
/// </summary>
/// <remarks>
/// Reachable without a session, which is the one deliberate exception in the product and is worth
/// stating plainly. A browser fetching an <c>&lt;img&gt;</c> cannot attach a bearer token, and the
/// alternatives are worse: fetching every picture through <c>HttpClient</c> into a blob would
/// defeat the lazy loading <c>L2-106 AC2</c> requires, and authenticating off the refresh cookie
/// would widen what that cookie is for.
/// <para>
/// What stands in for the session is the identifier: a version-4 GUID nobody can enumerate,
/// issued only to the member who uploaded it and disclosed only inside a congregation's own
/// responses. It is a capability, and it is treated as one — the response says
/// <c>Cache-Control: private</c> and carries no listing, member or congregation with it.
/// </para>
/// <para>
/// It declares <see cref="IAllowUnapproved"/> because the pipeline's membership check reads a
/// member that is not there. Nothing downstream reads a congregation either: the store is keyed
/// by the identifier alone.
/// </para>
/// </remarks>
public sealed record GetPhotoQuery(Guid PhotoId, PhotoSize Size) : IRequest<StoredPhoto>, IAllowUnapproved;
