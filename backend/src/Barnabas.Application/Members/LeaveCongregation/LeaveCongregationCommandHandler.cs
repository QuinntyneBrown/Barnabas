using Barnabas.Application.Common.Exceptions;
using Barnabas.Application.Common.Persistence;
using Barnabas.Application.Common.Tenancy;
using Barnabas.Domain.Listings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Barnabas.Application.Members.LeaveCongregation;

/// <summary>
/// Marks the member gone, takes their listings off the board, and ends their session.
/// </summary>
/// <remarks>
/// All three in one unit of work. A member recorded as gone whose listings were still on the
/// board would leave the congregation asking somebody who is no longer there; a member whose
/// session outlived their membership would still be reading the board.
/// <para>
/// The listings are archived rather than closed out. Nothing was lent, sold or given away — the
/// member simply left, and the wording should not claim otherwise.
/// </para>
/// </remarks>
public sealed class LeaveCongregationCommandHandler : IRequestHandler<LeaveCongregationCommand>
{
    private readonly IBarnabasDbContext _context;
    private readonly IAuthenticationStore _authentication;
    private readonly ICongregationContext _congregation;
    private readonly TimeProvider _time;

    public LeaveCongregationCommandHandler(
        IBarnabasDbContext context,
        IAuthenticationStore authentication,
        ICongregationContext congregation,
        TimeProvider time)
    {
        _context = context;
        _authentication = authentication;
        _congregation = congregation;
        _time = time;
    }

    public async Task Handle(LeaveCongregationCommand request, CancellationToken cancellationToken)
    {
        var memberId = _congregation.MemberId;
        var now = _time.GetUtcNow();

        var member = await _context.Members
            .FirstOrDefaultAsync(member => member.Id == memberId, cancellationToken)
            ?? throw new NotFoundException();

        var listings = await _context.Listings
            .Where(listing => listing.OwnerId == memberId && listing.Status == ListingStatus.Active)
            .ToListAsync(cancellationToken);

        foreach (var listing in listings)
        {
            listing.Archive(now);
        }

        member.Leave(now);

        await _context.SaveChangesAsync(cancellationToken);

        await _authentication.RevokeSessionAsync(_congregation.SessionId, now, cancellationToken);
    }
}
