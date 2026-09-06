namespace Barnabas.Domain.Moderation;

/// <summary>
/// Why somebody reported a listing.
/// </summary>
/// <remarks>
/// A short fixed set rather than free text, because a moderator reading a queue is triaging: what
/// they need first is which kind of problem this is. The note beside it is where the particulars
/// go.
/// <para>
/// These four, and no more, because they are the four the report dialogue offers. A reason nobody
/// can choose is a value the queue would have to render and never would.
/// </para>
/// </remarks>
public enum ReportReason
{
    /// <summary>Not something this board is for.</summary>
    NotAllowed = 0,

    /// <summary>Already lent, sold, given or booked, and still up.</summary>
    AlreadyGone = 1,

    /// <summary>What it says is not what it is.</summary>
    Misleading = 2,

    /// <summary>Something else, explained in the note.</summary>
    Other = 3,
}
