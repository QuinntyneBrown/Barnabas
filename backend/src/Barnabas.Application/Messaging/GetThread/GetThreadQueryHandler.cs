using Barnabas.Application.Common.Exceptions;
using Barnabas.Application.Common.Persistence;
using Barnabas.Application.Common.Tenancy;
using Barnabas.Domain.Messaging;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Barnabas.Application.Messaging.GetThread;

/// <summary>
/// Reads a thread the caller is party to, and moves their read mark.
/// </summary>
/// <remarks>
/// Party membership is checked here rather than declared through <c>IRequireOwnership</c>,
/// because a thread has two rightful actors rather than one owner. The ownership behaviour
/// answers "did this member create it", and the question here is "is this member one of the
/// two" - so the entity is asked instead.
/// <para>
/// A member who is not a party is told the thread was not found. A refusal would confirm it
/// exists, which is the disclosure the answer is avoiding.
/// </para>
/// </remarks>
public sealed class GetThreadQueryHandler : IRequestHandler<GetThreadQuery, ThreadDetailDto>
{
    private readonly IBarnabasDbContext _context;
    private readonly ICongregationContext _congregation;
    private readonly TimeProvider _time;

    public GetThreadQueryHandler(
        IBarnabasDbContext context,
        ICongregationContext congregation,
        TimeProvider time)
    {
        _context = context;
        _congregation = congregation;
        _time = time;
    }

    public async Task<ThreadDetailDto> Handle(GetThreadQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var caller = _congregation.MemberId;

        var thread = await _context.MessageThreads
            .Include(thread => thread.Messages)
            .Include(thread => thread.ReadMarks)
            .FirstOrDefaultAsync(thread => thread.Id == request.ThreadId, cancellationToken)
            ?? throw new NotFoundException();

        if (!thread.IsParty(caller))
        {
            throw new NotAPartyException(thread.Id);
        }

        // Reading it is what marks it read, and only for the member doing the reading.
        thread.MarkRead(Guid.NewGuid(), caller, _time.GetUtcNow());

        await _context.SaveChangesAsync(cancellationToken);

        var listing = await _context.Listings
            .FirstOrDefaultAsync(listing => listing.Id == thread.ListingId, cancellationToken);

        var listingRequest = await _context.ListingRequests
            .FirstOrDefaultAsync(candidate => candidate.Id == thread.RequestId, cancellationToken);

        var other = thread.OtherParty(caller);

        var names = await _context.Members
            .Where(member => member.Id == other || thread.Messages.Select(m => m.SenderId).Contains(member.Id))
            .ToDictionaryAsync(member => member.Id, member => member.DisplayName, cancellationToken);

        var messages = thread.Messages
            .OrderBy(message => message.SentAt)
            .Select(message => new MessageDto(
                message.Id,
                message.SenderId,
                names.GetValueOrDefault(message.SenderId, string.Empty),
                message.Body,
                message.SentAt,
                message.SenderId == caller))
            .ToList();

        return new ThreadDetailDto(
            thread.Id,
            thread.ListingId,
            listing?.Title ?? string.Empty,
            other,
            names.GetValueOrDefault(other, string.Empty),
            listingRequest?.Status ?? Domain.Requests.RequestStatus.Accepted,
            messages);
    }
}
