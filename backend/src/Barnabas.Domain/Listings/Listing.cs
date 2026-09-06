using Barnabas.Domain.Common;

namespace Barnabas.Domain.Listings;

/// <summary>
/// A post on the board. "Post" is the verb; a listing is the noun.
/// </summary>
public sealed class Listing : ITenantOwned, IOwnedResource
{
    public const int TitleMaxLength = 120;
    public const int DescriptionMaxLength = 4000;
    public const int ConditionMaxLength = 100;

    private readonly List<AvailabilityWindow> _availabilityWindows = [];

    private Listing()
    {
    }

    private Listing(
        Guid id,
        Guid congregationId,
        Guid ownerId,
        ListingKind kind,
        string title,
        string description,
        string category,
        string neighbourhood,
        DateTimeOffset postedAt)
    {
        Id = id;
        CongregationId = congregationId;
        OwnerId = ownerId;
        Kind = kind;
        Title = title;
        Description = description;
        Category = category;
        Neighbourhood = neighbourhood;
        PostedAt = postedAt;
        Status = ListingStatus.Active;
    }

    public Guid Id { get; private set; }

    public Guid CongregationId { get; private set; }

    /// <summary>The member who posted it. Never supplied by the client.</summary>
    public Guid OwnerId { get; private set; }

    public ListingKind Kind { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public string Category { get; private set; } = string.Empty;

    public string Neighbourhood { get; private set; } = string.Empty;

    public ListingStatus Status { get; private set; }

    public DateTimeOffset PostedAt { get; private set; }

    public DateTimeOffset? ClosedOutAt { get; private set; }

    /// <summary>When the owner shelved it. Null on a listing that was closed out instead.</summary>
    public DateTimeOffset? ArchivedAt { get; private set; }

    /// <summary>Present on a Lend listing and on no other kind.</summary>
    public LoanTerms? LoanTerms { get; private set; }

    /// <summary>Present on a Sell listing and on no other kind. A stated asking figure only.</summary>
    public decimal? Price { get; private set; }

    /// <summary>Present on a Sell listing and on no other kind. What state the thing is in.</summary>
    public string? Condition { get; private set; }

    /// <summary>
    /// Present on a Help listing and on no other kind. The periods the owner is free.
    /// </summary>
    /// <remarks>
    /// A Help listing declares at least one; the factory refuses to make one without. A request
    /// chooses among these by identifier, and <see cref="DeclaresWindow"/> is what lets a
    /// handler refuse a window this listing never offered.
    /// </remarks>
    public IReadOnlyList<AvailabilityWindow> AvailabilityWindows => _availabilityWindows;

    /// <summary>Whether this listing declared the window with the given identifier.</summary>
    public bool DeclaresWindow(Guid availabilityWindowId) =>
        _availabilityWindows.Exists(window => window.Id == availabilityWindowId);

    public bool IsActive => Status == ListingStatus.Active;

    public bool IsOwnedBy(Guid memberId) => OwnerId == memberId;

    /// <summary>
    /// Posts a Lend listing. The item stays the owner's, so the terms carry the date it
    /// comes back and there is no price.
    /// </summary>
    public static Listing PostLend(
        Guid id,
        Guid congregationId,
        Guid ownerId,
        string title,
        string description,
        string category,
        string neighbourhood,
        DateOnly returnBy,
        DateTimeOffset postedAt) =>
        new(id, congregationId, ownerId, ListingKind.Lend, title, description, category, neighbourhood, postedAt)
        {
            LoanTerms = new LoanTerms(returnBy),
        };

    /// <summary>
    /// Posts a Give listing. Ownership transfers and pickup is arranged by the members,
    /// so there is no price and no return date.
    /// </summary>
    /// <remarks>
    /// Feature slice 1 exposes no endpoint for this kind — <c>L2-028</c> is deferred. The
    /// factory exists because <see cref="CloseOut"/> maps every kind to its own outcome and
    /// <c>L2-037</c> tests all four of those outcomes, which needs a listing of each kind to
    /// exist. The same is true of <see cref="PostSell"/> and <see cref="PostHelp"/>.
    /// </remarks>
    public static Listing PostGive(
        Guid id,
        Guid congregationId,
        Guid ownerId,
        string title,
        string description,
        string category,
        string neighbourhood,
        DateTimeOffset postedAt) =>
        new(id, congregationId, ownerId, ListingKind.Give, title, description, category, neighbourhood, postedAt);

    /// <summary>
    /// Posts a Sell listing. The price is a stated asking figure; Barnabas never collects,
    /// holds, or transmits payment.
    /// </summary>
    public static Listing PostSell(
        Guid id,
        Guid congregationId,
        Guid ownerId,
        string title,
        string description,
        string category,
        string neighbourhood,
        string condition,
        decimal price,
        DateTimeOffset postedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(condition);
        ArgumentOutOfRangeException.ThrowIfNegative(price);

        return new Listing(
            id, congregationId, ownerId, ListingKind.Sell, title, description, category, neighbourhood, postedAt)
        {
            Condition = condition,
            Price = price,
        };
    }

    /// <summary>
    /// Posts a Help listing. Help offers time rather than an object, so it carries no price
    /// and no photo.
    /// </summary>
    /// <remarks>
    /// At least one window is required and the factory refuses without one, because an offer of
    /// time nobody can name a moment for is not an offer. <c>L2-030</c>.
    /// </remarks>
    public static Listing PostHelp(
        Guid id,
        Guid congregationId,
        Guid ownerId,
        string title,
        string description,
        string category,
        string neighbourhood,
        IReadOnlyCollection<AvailabilityWindow> windows,
        DateTimeOffset postedAt)
    {
        ArgumentNullException.ThrowIfNull(windows);

        if (windows.Count == 0)
        {
            throw new ArgumentException("A Help listing declares at least one window.", nameof(windows));
        }

        var listing = new Listing(
            id, congregationId, ownerId, ListingKind.Help, title, description, category, neighbourhood, postedAt);

        listing._availabilityWindows.AddRange(windows);

        return listing;
    }

    /// <summary>
    /// Records that the listing has served its purpose, in the wording belonging to its kind.
    /// </summary>
    /// <remarks>
    /// Takes no status argument. The entity reads its own kind and chooses the outcome, so
    /// a client cannot mark a Give listing sold. The screen labels its button from the same
    /// kind — <em>Mark as taken</em>, <em>Mark as sold</em>, <em>Mark as booked</em> — but
    /// the two derive from the rule independently rather than the server trusting the
    /// client's word for it. This is L2-037.
    /// </remarks>
    /// <summary>
    /// Corrects the listing's details. The kind is not among them.
    /// </summary>
    /// <remarks>
    /// A listing's kind decides its fields, its request form and its close-out verb, and requests
    /// already made against it were made on the strength of that kind. Changing it would leave
    /// all of those describing something else, so there is no parameter for it here and the
    /// command refuses one at the edge.
    /// </remarks>
    public void Edit(string title, string description, string category, string neighbourhood)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        if (!IsActive)
        {
            throw new ListingNotActiveException(Id);
        }

        Title = title;
        Description = description;
        Category = category;
        Neighbourhood = neighbourhood;
    }

    /// <summary>
    /// Takes the listing off the board without closing it out.
    /// </summary>
    /// <remarks>
    /// Shelving, not finishing. The owner has not lent, sold or given the thing away; they have
    /// simply stopped offering it for now, and <see cref="Restore"/> puts it back.
    /// </remarks>
    public void Archive(DateTimeOffset asOf)
    {
        if (!IsActive)
        {
            throw new ListingNotActiveException(Id);
        }

        Status = ListingStatus.Archived;
        ArchivedAt = asOf;
    }

    /// <summary>
    /// Whether this listing may go back on the board.
    /// </summary>
    /// <remarks>
    /// A Lend listing that has been closed out is <see cref="ListingStatus.Archived"/>, and so is
    /// one that was merely shelved - the status cannot tell them apart. <see cref="ClosedOutAt"/>
    /// can: a returned ladder has served its purpose, and putting it back on the board would be
    /// reopening a finished loan. This is the rule L2-037 and L2-039 would otherwise disagree
    /// about.
    /// </remarks>
    public bool CanBeRestored => Status == ListingStatus.Archived && ClosedOutAt is null;

    /// <summary>Puts a shelved listing back on the board.</summary>
    public void Restore()
    {
        if (!CanBeRestored)
        {
            throw new ListingNotRestorableException(Id);
        }

        Status = ListingStatus.Active;
        ArchivedAt = null;
    }

    public void CloseOut(DateTimeOffset asOf)
    {
        if (!IsActive)
        {
            throw new ListingNotActiveException(Id);
        }

        Status = OutcomeFor(Kind);
        ClosedOutAt = asOf;
    }

    /// <summary>
    /// The status a listing of the given kind reaches when it is closed out.
    /// </summary>
    public static ListingStatus OutcomeFor(ListingKind kind) => kind switch
    {
        ListingKind.Sell => ListingStatus.Sold,
        ListingKind.Give => ListingStatus.GivenAway,
        ListingKind.Lend => ListingStatus.Archived,
        ListingKind.Help => ListingStatus.Completed,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown listing kind."),
    };
}
