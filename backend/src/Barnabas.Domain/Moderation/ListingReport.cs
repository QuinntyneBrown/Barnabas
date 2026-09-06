using Barnabas.Domain.Common;

namespace Barnabas.Domain.Moderation;

/// <summary>
/// One member's complaint about one listing.
/// </summary>
/// <remarks>
/// The reporter is recorded and kept, because a moderator reading the queue needs to know who is
/// asking — but nothing that reaches the reported member ever carries it. That is <c>L2-081</c>,
/// and it is held by the shape of the projections rather than by a flag on this row: the member's
/// own view of a listing has no reporter field to fill in.
/// <para>
/// A report is resolved rather than deleted. Clearing the flag and leaving the reports behind
/// would mean a later report re-flagged the listing and dragged the settled complaints back into
/// the queue with it.
/// </para>
/// </remarks>
public sealed class ListingReport : ITenantOwned
{
    public const int NoteMaxLength = 1000;

    private ListingReport()
    {
    }

    public ListingReport(
        Guid id,
        Guid congregationId,
        Guid listingId,
        Guid reporterId,
        ReportReason reason,
        string? note,
        DateTimeOffset reportedAt)
    {
        Id = id;
        CongregationId = congregationId;
        ListingId = listingId;
        ReporterId = reporterId;
        Reason = reason;
        Note = note;
        ReportedAt = reportedAt;
    }

    public Guid Id { get; private set; }

    public Guid CongregationId { get; private set; }

    public Guid ListingId { get; private set; }

    /// <summary>Who complained. Disclosed to moderators and to nobody else.</summary>
    public Guid ReporterId { get; private set; }

    public ReportReason Reason { get; private set; }

    /// <summary>What they wanted to add, if anything.</summary>
    public string? Note { get; private set; }

    public DateTimeOffset ReportedAt { get; private set; }

    public DateTimeOffset? ResolvedAt { get; private set; }

    public Guid? ResolvedByMemberId { get; private set; }

    public ModerationOutcome? Outcome { get; private set; }

    public bool IsOpen => ResolvedAt is null;

    /// <summary>A moderator has decided. An already-settled report is left as it was.</summary>
    public void Resolve(ModerationOutcome outcome, Guid moderatorId, DateTimeOffset asOf)
    {
        if (!IsOpen)
        {
            return;
        }

        Outcome = outcome;
        ResolvedByMemberId = moderatorId;
        ResolvedAt = asOf;
    }
}
