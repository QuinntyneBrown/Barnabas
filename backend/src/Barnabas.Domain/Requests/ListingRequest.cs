using Barnabas.Domain.Common;
using Barnabas.Domain.Listings;

namespace Barnabas.Domain.Requests;

/// <summary>
/// A member's ask against one listing, carrying a message to the owner and the terms that
/// kind of listing needs. It is the only thing that opens a message thread.
/// </summary>
public sealed class ListingRequest : ITenantOwned
{
    public const int MessageMaxLength = 4000;

    private ListingRequest()
    {
    }

    private ListingRequest(
        Guid id,
        Guid congregationId,
        Guid listingId,
        Guid requesterId,
        string message,
        DateTimeOffset madeAt)
    {
        Id = id;
        CongregationId = congregationId;
        ListingId = listingId;
        RequesterId = requesterId;
        Message = message;
        MadeAt = madeAt;
        Status = RequestStatus.Pending;
    }

    public Guid Id { get; private set; }

    public Guid CongregationId { get; private set; }

    public Guid ListingId { get; private set; }

    /// <summary>The member who asked. Taken from the session, never from the client.</summary>
    public Guid RequesterId { get; private set; }

    public string Message { get; private set; } = string.Empty;

    public RequestStatus Status { get; private set; }

    public DateTimeOffset MadeAt { get; private set; }

    public DateTimeOffset? DecidedAt { get; private set; }

    /// <summary>Present on a request against a Lend listing and on no other kind.</summary>
    public LoanRequestTerms? LoanTerms { get; private set; }

    /// <summary>
    /// Concurrency token, checked on save.
    /// </summary>
    /// <remarks>
    /// One owner in two tabs can load the same pending request and accept in one while
    /// declining in the other. Without this token both would commit and the last write
    /// would silently win. L2-060 requires exactly one transition to survive, and the
    /// status check below cannot deliver that on its own — it runs before the save.
    /// </remarks>
    public byte[] RowVersion { get; private set; } = [];

    public bool IsPending => Status == RequestStatus.Pending;

    public static ListingRequest MakeLoanRequest(
        Guid id,
        Guid congregationId,
        Guid listingId,
        Guid requesterId,
        string message,
        DateOnly pickupOn,
        DateOnly returnBy,
        DateTimeOffset madeAt) =>
        new(id, congregationId, listingId, requesterId, message, madeAt)
        {
            LoanTerms = new LoanRequestTerms(pickupOn, returnBy),
        };

    /// <summary>
    /// The owner agrees. The thread that follows is opened by the handler in the same unit
    /// of work, because a committed acceptance without a thread would leave the requester
    /// notified of a decision they cannot act on.
    /// </summary>
    public void Accept(DateTimeOffset asOf) => Decide(RequestStatus.Accepted, asOf);

    /// <summary>
    /// The owner cannot or does not wish to fulfil the ask. The request is retained rather
    /// than deleted, so the requester can see what became of it. No thread is opened.
    /// </summary>
    public void Decline(DateTimeOffset asOf) => Decide(RequestStatus.Declined, asOf);

    private void Decide(RequestStatus decision, DateTimeOffset asOf)
    {
        if (!IsPending)
        {
            throw new RequestAlreadyDecidedException(Id, Status);
        }

        Status = decision;
        DecidedAt = asOf;
    }
}
