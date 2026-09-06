using Barnabas.Application.Common.Exceptions;
using Barnabas.Application.Common.Persistence;
using Barnabas.Domain.Listings;
using Barnabas.Domain.Members;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Barnabas.Application.Members.GetMemberProfile;

/// <summary>
/// Reads another member's public profile, and what they currently have on the board.
/// </summary>
/// <remarks>
/// Approved members only, and through the filtered set — so somebody in another congregation,
/// somebody still waiting on a moderator, and somebody who has left are all simply not found. One
/// answer for three conditions, because distinguishing them would say who belongs where.
/// </remarks>
public sealed class GetMemberProfileQueryHandler : IRequestHandler<GetMemberProfileQuery, MemberProfileDto>
{
    private readonly IBarnabasDbContext _context;

    public GetMemberProfileQueryHandler(IBarnabasDbContext context) => _context = context;

    public async Task<MemberProfileDto> Handle(
        GetMemberProfileQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var member = await _context.Members
            .Include(member => member.HelpTags)
            .FirstOrDefaultAsync(
                member => member.Id == request.MemberId && member.Status == MemberStatus.Approved,
                cancellationToken)
            ?? throw new NotFoundException();

        var listings = await _context.Listings
            .Where(listing => listing.OwnerId == member.Id && listing.Status == ListingStatus.Active)
            .OrderByDescending(listing => listing.PostedAt)
            .Select(listing => new ProfileListingDto(listing.Id, listing.Kind, listing.Title, listing.Price))
            .ToListAsync(cancellationToken);

        return new MemberProfileDto(
            member.Id,
            member.DisplayName,
            member.Neighbourhood,
            member.Description,
            [.. member.HelpTags.Select(tag => tag.Tag)],
            listings);
    }
}
