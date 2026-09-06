using Barnabas.Application.Common.Exceptions;
using Barnabas.Application.Common.Persistence;
using Barnabas.Application.Common.Tenancy;
using Barnabas.Application.Photos.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Barnabas.Application.Members.EraseMyData;

/// <summary>
/// Anonymises the member, their listings and their messages, and ends their session.
/// </summary>
/// <remarks>
/// Anonymised in place rather than deleted, throughout. Rows here are pointed at by rows belonging
/// to other members — a thread points at a request, which points at a listing, which points at the
/// member — and deleting any of them would take away the other party's record of a conversation
/// they are entitled to keep. <c>L2-101 AC2</c> asks for "removed or irreversibly anonymised", and
/// this is the second of those on purpose: <c>L2-066 AC1</c> requires the surviving side of a
/// thread to stay legible.
/// <para>
/// Immediate rather than scheduled. "Within the documented period" is satisfied most simply by a
/// documented period of none, and a queue nobody drains is how erasure quietly stops happening.
/// </para>
/// <para>
/// The photographs go for real. Bytes in a folder are pointed at by nothing and are the one part
/// of a member's data with no reason to survive them.
/// </para>
/// </remarks>
public sealed class EraseMyDataCommandHandler : IRequestHandler<EraseMyDataCommand, ErasedDataResult>
{
    private readonly IBarnabasDbContext _context;
    private readonly IAuthenticationStore _authentication;
    private readonly ICongregationContext _congregation;
    private readonly IPhotoStore _photos;
    private readonly TimeProvider _time;

    public EraseMyDataCommandHandler(
        IBarnabasDbContext context,
        IAuthenticationStore authentication,
        ICongregationContext congregation,
        IPhotoStore photos,
        TimeProvider time)
    {
        _context = context;
        _authentication = authentication;
        _congregation = congregation;
        _photos = photos;
        _time = time;
    }

    public async Task<ErasedDataResult> Handle(
        EraseMyDataCommand request,
        CancellationToken cancellationToken)
    {
        var memberId = _congregation.MemberId;
        var now = _time.GetUtcNow();

        var member = await _context.Members
            .FirstOrDefaultAsync(candidate => candidate.Id == memberId, cancellationToken)
            ?? throw new NotFoundException();

        var listings = await _context.Listings
            .Where(listing => listing.OwnerId == memberId)
            .ToListAsync(cancellationToken);

        var messages = await _context.Messages
            .Where(message => message.SenderId == memberId)
            .ToListAsync(cancellationToken);

        var photoIds = listings.Where(listing => listing.PhotoId is not null)
            .Select(listing => listing.PhotoId!.Value)
            .ToList();

        foreach (var listing in listings)
        {
            listing.EraseOnRequest(now);
        }

        foreach (var message in messages)
        {
            message.Erase();
        }

        // The notifications naming them are other members' rows and are erased rather than kept:
        // a notification saying "Priya asked about your ladder" is Priya's name in somebody else's
        // list, which is exactly what she asked to have removed.
        var namingThem = await _context.Notifications
            .Where(notification => notification.SubjectMemberId == memberId)
            .ToListAsync(cancellationToken);

        foreach (var notification in namingThem)
        {
            notification.ForgetSubject();
        }

        member.Erase(now);

        await _context.SaveChangesAsync(cancellationToken);

        // After the save. Deleting the bytes first would mean a save that failed had already
        // destroyed them.
        foreach (var photoId in photoIds)
        {
            await _photos.DeleteAsync(photoId, cancellationToken);
        }

        await _authentication.RevokeAllSessionsAsync(memberId, now, cancellationToken);

        return new ErasedDataResult(member.Id, listings.Count, messages.Count, now);
    }
}
