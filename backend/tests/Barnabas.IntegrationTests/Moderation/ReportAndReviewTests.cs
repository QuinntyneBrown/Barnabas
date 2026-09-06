using System.Net;
using Barnabas.Domain.Listings;
using Barnabas.Domain.Moderation;
using Barnabas.Domain.Notifications;
using Barnabas.IntegrationTests.Fixtures;
using Barnabas.Infrastructure.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;

namespace Barnabas.IntegrationTests.Moderation;

// Acceptance Test
// Traces to: L2-080, L2-081, L2-082, L2-083, L2-084
// Description: A member reports a listing and it is marked for a moderator without leaving the
// board. A moderator sees the queue with the reason and the poster; nobody else sees it at all.
// Approving clears the flag and changes nothing; removing takes it off the board and tells its
// owner, and neither reaches a listing in another congregation.
public sealed class ReportAndReviewTests : AcceptanceTest
{
    public ReportAndReviewTests(BarnabasApiFactory api)
        : base(api)
    {
    }

    // L2-080 AC1: Given an active listing, when a member reports it with a reason, then a report
    // is recorded and the listing is marked Flagged for moderators.
    [Fact]
    public async Task A_report_is_recorded_and_the_listing_is_marked_for_a_moderator()
    {
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        var reported = await ReportAsync(priya, SeedData.Listings.Ladder);

        reported.ListingId.ShouldBe(SeedData.Listings.Ladder);
        reported.ReportId.ShouldNotBe(Guid.Empty);

        (await CountReportsAsync(SeedData.Listings.Ladder)).ShouldBe(1);

        var queue = await QueueAsync(marion);

        queue.ShouldContain(flagged => flagged.ListingId == SeedData.Listings.Ladder);

        // Marked, not withdrawn. A complaint is not a verdict, so the ladder is still on the board
        // for everybody who was looking at it a moment ago.
        (await BoardAsync(priya)).Listings
            .ShouldContain(listing => listing.ListingId == SeedData.Listings.Ladder);
    }

    // L2-080 AC2: Given a listing a member has already reported, when they report it again, then
    // the response is 409 Conflict and no second report is recorded.
    [Fact]
    public async Task The_same_member_cannot_report_the_same_listing_twice()
    {
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        await ReportAsync(priya, SeedData.Listings.Ladder);

        var again = await ReportingAsync(priya, SeedData.Listings.Ladder);

        again.StatusCode.ShouldBe(HttpStatusCode.Conflict);

        // Decided by the unique index on (ListingId, ReporterId), which is why the count is the
        // assertion that matters: a handler check would leave two rows under concurrency.
        (await CountReportsAsync(SeedData.Listings.Ladder)).ShouldBe(1);
    }

    // Another member may still object to the same listing. "Already reported" is about one
    // person's complaint, not about the listing having been complained about.
    [Fact]
    public async Task A_second_member_may_report_the_same_listing()
    {
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);
        using var grace = await Api.ClientForAsync(SeedData.Grace.Id);
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        await ReportAsync(priya, SeedData.Listings.Ladder);
        await ReportAsync(grace, SeedData.Listings.Ladder, ReportReason.Misleading, "It is not aluminium.");

        (await CountReportsAsync(SeedData.Listings.Ladder)).ShouldBe(2);

        // One row in the queue carrying both complaints, rather than the same listing twice.
        var flagged = (await QueueAsync(marion)).ShouldHaveSingleItem();

        flagged.Reports.Count.ShouldBe(2);
    }

    // L2-081 AC1: Given a flagged listing, when its owner requests it, then no reporter identity
    // is returned.
    [Fact]
    public async Task The_owner_of_a_reported_listing_learns_nothing_about_who_reported_it()
    {
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        await ReportAsync(priya, SeedData.Listings.Ladder, ReportReason.AlreadyGone, "It went last week.");

        var response = await marion.GetAsync(
            $"/listings/{SeedData.Listings.Ladder}",
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var body = await response.ReadBodyAsync();

        // Not "no reporter field" but "nothing that could identify one": neither Priya's
        // identifier, nor her name, nor the note she wrote, appears anywhere in what the owner is
        // handed. The owner is not even told it was reported.
        body.ShouldNotContain(SeedData.Priya.Id.ToString());
        body.ShouldNotContain(SeedData.Priya.DisplayName);
        body.ShouldNotContain("It went last week.");
        body.ShouldNotContain("report", Case.Insensitive);
        body.ShouldNotContain("flag", Case.Insensitive);
    }

    // L2-081 AC2: Given a flagged listing, when a moderator requests the moderation queue, then
    // the reason is returned and the reporter identity is available only to moderators.
    [Fact]
    public async Task A_moderator_is_told_the_reason_and_who_gave_it()
    {
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        await ReportAsync(priya, SeedData.Listings.Ladder, ReportReason.Misleading, "Rung is cracked.");

        var flagged = (await QueueAsync(marion)).ShouldHaveSingleItem();

        var report = flagged.Reports.ShouldHaveSingleItem();

        report.Reason.ShouldBe(nameof(ReportReason.Misleading));
        report.Note.ShouldBe("Rung is cracked.");
        report.ReporterId.ShouldBe(SeedData.Priya.Id);
        report.ReporterDisplayName.ShouldBe(SeedData.Priya.DisplayName);
    }

    // L2-082 AC1: Given three flagged listings, when a moderator requests the queue, then all
    // three are returned with their reason and poster.
    [Fact]
    public async Task The_queue_holds_every_flagged_listing_with_its_reason_and_poster()
    {
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);
        using var grace = await Api.ClientForAsync(SeedData.Grace.Id);
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        var gracesListing = await PostAGiveListingAsync(grace);

        await ReportAsync(priya, SeedData.Listings.Ladder);
        await ReportAsync(priya, SeedData.Listings.Drill, ReportReason.Misleading);
        await ReportAsync(priya, gracesListing, ReportReason.NotAllowed);

        var queue = await QueueAsync(marion);

        queue.Count.ShouldBe(3);

        queue.Select(flagged => flagged.ListingId)
            .ShouldBe([SeedData.Listings.Ladder, SeedData.Listings.Drill, gracesListing], ignoreOrder: true);

        // Each row names its poster, so a moderator is not left holding an identifier.
        queue.Single(flagged => flagged.ListingId == gracesListing)
            .OwnerDisplayName.ShouldBe(SeedData.Grace.DisplayName);

        queue.Single(flagged => flagged.ListingId == SeedData.Listings.Drill)
            .Reports.ShouldHaveSingleItem()
            .Reason.ShouldBe(nameof(ReportReason.Misleading));
    }

    // L2-082 AC2: Given a member who is not a moderator, when they request the queue, then the
    // response is 403 Forbidden.
    [Fact]
    public async Task An_ordinary_member_may_not_read_the_queue()
    {
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        await ReportAsync(priya, SeedData.Listings.Ladder);

        var response = await priya.GetAsync("/moderation/listings", TestContext.Current.CancellationToken);

        // Forbidden rather than not found: the role is a property of the caller, so refusing it
        // discloses nothing about what exists.
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    // L2-083 AC1: Given a flagged listing, when a moderator approves it, then its Flagged mark is
    // cleared and it remains on the board.
    [Fact]
    public async Task Approving_clears_the_flag_and_leaves_the_listing_on_the_board()
    {
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        await ReportAsync(priya, SeedData.Listings.Ladder);

        var reviewed = await DecideAsync(marion, SeedData.Listings.Ladder, "approve");

        reviewed.Flagged.ShouldBeFalse();
        reviewed.Status.ShouldBe(nameof(ListingStatus.Active));

        (await QueueAsync(marion)).ShouldBeEmpty();

        (await BoardAsync(priya)).Listings
            .ShouldContain(listing => listing.ListingId == SeedData.Listings.Ladder);

        // The complaint is settled rather than deleted, so a later one flags the listing afresh
        // without dragging the reviewed one back into the queue behind it.
        (await CountOpenReportsAsync(SeedData.Listings.Ladder)).ShouldBe(0);
        (await CountReportsAsync(SeedData.Listings.Ladder)).ShouldBe(1);
    }

    [Fact]
    public async Task A_listing_nobody_reported_cannot_be_approved()
    {
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        var response = await marion.PostJsonAsync(
            $"/moderation/listings/{SeedData.Listings.Ladder}/approve",
            new { },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    // L2-084 AC1: Given a flagged listing, when a moderator removes it, then it no longer appears
    // on the board and its owner is notified.
    [Fact]
    public async Task Removing_takes_the_listing_off_the_board_and_tells_its_owner()
    {
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        await ReportAsync(priya, SeedData.Listings.Ladder, ReportReason.NotAllowed);

        var reviewed = await DecideAsync(marion, SeedData.Listings.Ladder, "remove");

        reviewed.Status.ShouldBe(nameof(ListingStatus.Removed));
        reviewed.Flagged.ShouldBeFalse();

        (await BoardAsync(priya)).Listings
            .ShouldNotContain(listing => listing.ListingId == SeedData.Listings.Ladder);

        (await QueueAsync(marion)).ShouldBeEmpty();

        var hers = await NotificationsOfAsync(marion);

        hers.ShouldContain(notification => notification.Kind == nameof(NotificationKind.ListingRemoved));

        // What was removed, and nothing about who objected to it - the other half of L2-081.
        var removal = hers.Single(notification => notification.Kind == nameof(NotificationKind.ListingRemoved));

        removal.ListingId.ShouldBe(SeedData.Listings.Ladder);
        removal.ListingTitle.ShouldBe(SeedData.Listings.LadderTitle);
        removal.SubjectMemberId.ShouldBeNull();
        removal.SubjectMemberDisplayName.ShouldBeNull();
    }

    // L2-084 AC2: Given a listing in another congregation, when a moderator attempts to remove it,
    // then the response is 404 Not Found.
    [Fact]
    public async Task A_moderator_cannot_reach_across_the_congregation_boundary()
    {
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        var removed = await marion.PostJsonAsync(
            $"/moderation/listings/{SeedData.Listings.Canoe}/remove",
            new { },
            TestContext.Current.CancellationToken);

        // Not found rather than forbidden. A moderator's role widens what they may do with what
        // they can already see; it does not widen what they can see.
        removed.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        (await StatusOfAsync(SeedData.Listings.Canoe)).ShouldBe(ListingStatus.Active);
    }

    // A member of another congregation cannot report across the boundary either, and gets the
    // same 404 every other cross-congregation read gets - L2-089.
    [Fact]
    public async Task A_listing_in_another_congregation_cannot_be_reported()
    {
        using var hank = await Api.ClientForAsync(SeedData.Hank.Id);

        var response = await ReportingAsync(hank, SeedData.Listings.Ladder);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        (await CountReportsAsync(SeedData.Listings.Ladder)).ShouldBe(0);
    }

    // The interaction the plan's addendum flagged: a removed listing must not be restorable, or
    // L2-039 would be an undo button for a moderator's decision.
    [Fact]
    public async Task An_owner_cannot_put_a_removed_listing_back_on_the_board()
    {
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        await ReportAsync(priya, SeedData.Listings.Ladder);
        await DecideAsync(marion, SeedData.Listings.Ladder, "remove");

        var restored = await marion.PostJsonAsync(
            $"/listings/{SeedData.Listings.Ladder}/restore",
            new { },
            TestContext.Current.CancellationToken);

        restored.StatusCode.ShouldBe(HttpStatusCode.Conflict);

        (await StatusOfAsync(SeedData.Listings.Ladder)).ShouldBe(ListingStatus.Removed);
    }

    // A report carrying a card number is refused by name rather than quietly dropped. The note is
    // free text, which is exactly where somebody would paste the thing they were complaining about.
    [Fact]
    public async Task A_report_offering_payment_details_is_refused_naming_the_field()
    {
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        var response = await priya.PostJsonAsync(
            $"/listings/{SeedData.Listings.Ladder}/reports",
            new
            {
                reason = nameof(ReportReason.Misleading),
                note = "They asked me to pay up front.",
                cardNumber = "4111111111111111",
            },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.ReadBodyAsync()).ShouldContain("cardNumber");

        (await CountReportsAsync(SeedData.Listings.Ladder)).ShouldBe(0);
    }

    private static async Task<ReportedListing> ReportAsync(
        HttpClient client,
        Guid listingId,
        ReportReason reason = ReportReason.NotAllowed,
        string? note = null)
    {
        var response = await ReportingAsync(client, listingId, reason, note);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        return await response.ReadAsync<ReportedListing>();
    }

    private static async Task<HttpResponseMessage> ReportingAsync(
        HttpClient client,
        Guid listingId,
        ReportReason reason = ReportReason.NotAllowed,
        string? note = null) =>
        await client.PostJsonAsync(
            $"/listings/{listingId}/reports",
            new { reason = reason.ToString(), note },
            TestContext.Current.CancellationToken);

    private static async Task<IReadOnlyList<FlaggedListingBody>> QueueAsync(HttpClient client)
    {
        var response = await client.GetAsync("/moderation/listings", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        return await response.ReadAsync<IReadOnlyList<FlaggedListingBody>>();
    }

    private static async Task<ReviewedListing> DecideAsync(HttpClient client, Guid listingId, string decision)
    {
        var response = await client.PostJsonAsync(
            $"/moderation/listings/{listingId}/{decision}",
            new { },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        return await response.ReadAsync<ReviewedListing>();
    }

    private static async Task<BoardPageBody> BoardAsync(HttpClient client) =>
        await (await client.GetAsync("/board", TestContext.Current.CancellationToken))
            .ReadAsync<BoardPageBody>();

    private static async Task<IReadOnlyList<NotificationBody>> NotificationsOfAsync(HttpClient client) =>
        await (await client.GetAsync("/notifications", TestContext.Current.CancellationToken))
            .ReadAsync<IReadOnlyList<NotificationBody>>();

    private static async Task<Guid> PostAGiveListingAsync(HttpClient client)
    {
        var response = await client.PostJsonAsync(
            "/listings/give",
            new
            {
                title = "Box of jam jars",
                description = "Washed and lidded, about forty of them.",
                category = "Kitchen",
                neighbourhood = SeedData.Grace.Neighbourhood,
            },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        return (await response.ReadAsync<PostedListing>()).ListingId;
    }

    private Task<int> CountReportsAsync(Guid listingId) =>
        Api.QueryAsync(context => context
            .Set<ListingReport>()
            .IgnoreQueryFilters()
            .CountAsync(report => report.ListingId == listingId));

    private Task<int> CountOpenReportsAsync(Guid listingId) =>
        Api.QueryAsync(context => context
            .Set<ListingReport>()
            .IgnoreQueryFilters()
            .CountAsync(report => report.ListingId == listingId && report.ResolvedAt == null));

    private Task<ListingStatus> StatusOfAsync(Guid listingId) =>
        Api.QueryAsync(context => context
            .Set<Listing>()
            .IgnoreQueryFilters()
            .Where(listing => listing.Id == listingId)
            .Select(listing => listing.Status)
            .SingleAsync());
}
