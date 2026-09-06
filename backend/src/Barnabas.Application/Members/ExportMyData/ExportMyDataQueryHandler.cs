using Barnabas.Application.Common.Exceptions;
using Barnabas.Application.Common.Persistence;
using Barnabas.Application.Common.Tenancy;
using Barnabas.Application.Photos.Common;
using Barnabas.Domain.Photos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Barnabas.Application.Members.ExportMyData;

/// <summary>
/// Gathers the four things a member wrote, and their own record.
/// </summary>
/// <remarks>
/// Four reads rather than a join, because they are four unrelated collections and joining them
/// would produce a cross product to be untangled afterwards. Every one goes through the
/// congregation filter, so an export is of one membership rather than of a person.
/// </remarks>
public sealed class ExportMyDataQueryHandler : IRequestHandler<ExportMyDataQuery, MyDataExport>
{
    private readonly IBarnabasDbContext _context;
    private readonly ICongregationContext _congregation;
    private readonly TimeProvider _time;

    public ExportMyDataQueryHandler(
        IBarnabasDbContext context,
        ICongregationContext congregation,
        TimeProvider time)
    {
        _context = context;
        _congregation = congregation;
        _time = time;
    }

    public async Task<MyDataExport> Handle(ExportMyDataQuery request, CancellationToken cancellationToken)
    {
        var memberId = _congregation.MemberId;

        var member = await _context.Members
            .FirstOrDefaultAsync(candidate => candidate.Id == memberId, cancellationToken)
            ?? throw new NotFoundException();

        var congregationName = await _context.Congregations
            .Where(congregation => congregation.Id == member.CongregationId)
            .Select(congregation => congregation.Name)
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

        var listings = await _context.Listings
            .Where(listing => listing.OwnerId == memberId)
            .OrderBy(listing => listing.PostedAt)
            .ToListAsync(cancellationToken);

        var requests = await _context.ListingRequests
            .Where(listingRequest => listingRequest.RequesterId == memberId)
            .Join(
                _context.Listings,
                listingRequest => listingRequest.ListingId,
                listing => listing.Id,
                (listingRequest, listing) => new { Request = listingRequest, listing.Title })
            .OrderBy(row => row.Request.MadeAt)
            .ToListAsync(cancellationToken);

        var messages = await _context.Messages
            .Where(message => message.SenderId == memberId)
            .OrderBy(message => message.SentAt)
            .ToListAsync(cancellationToken);

        return new MyDataExport(
            _time.GetUtcNow(),
            new ExportedProfile(
                member.Id,
                member.DisplayName,
                member.EmailAddress,
                member.Neighbourhood,
                member.Description,
                member.HelpTags.Select(tag => tag.Tag).ToList(),
                member.Role.ToString(),
                member.Status.ToString(),
                member.ReasonForJoining,
                congregationName),
            listings.ConvertAll(listing => new ExportedListing(
                listing.Id,
                listing.Kind.ToString(),
                listing.Title,
                listing.Description,
                listing.Category,
                listing.Neighbourhood,
                listing.Status.ToString(),
                listing.Price,
                listing.PostedAt,
                listing.PhotoId is { } photoId ? PhotoUrl.For(photoId, PhotoSize.Full) : null)),
            requests.ConvertAll(row => new ExportedRequest(
                row.Request.Id,
                row.Request.ListingId,
                row.Title,
                row.Request.Message,
                row.Request.Status.ToString(),
                row.Request.MadeAt)),
            messages.ConvertAll(message => new ExportedMessage(
                message.Id,
                message.ThreadId,
                message.Body,
                message.SentAt)));
    }
}
