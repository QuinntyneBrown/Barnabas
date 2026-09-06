using System.Net;
using Barnabas.Domain.Listings;
using Barnabas.IntegrationTests.Fixtures;
using Barnabas.Infrastructure.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;

namespace Barnabas.IntegrationTests.Listings;

// Acceptance Test
// Traces to: L2-036, L2-038, L2-039, L2-040
// Description: An owner keeps their own listings in order - correcting one, taking one off the
// board without closing it out, putting it back, and removing one for good.
//
// Archive and close-out both leave a Lend listing Archived, and only one of them may be undone.
// The distinction is ClosedOutAt, and restore refuses a listing that has already served its
// purpose.
public sealed class ListingLifecycleTests : AcceptanceTest
{
    public ListingLifecycleTests(BarnabasApiFactory api)
        : base(api)
    {
    }

    // L2-036 AC1: Given a member's own active listing, when they submit a changed title, then the
    // change is persisted.
    [Fact]
    public async Task An_owner_changes_the_title_of_their_own_listing()
    {
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        var response = await marion.PutJsonAsync(
            $"/listings/{SeedData.Listings.Ladder}",
            ALadder(title: "Extending ladder, three sections, fibreglass"),
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var detail = await ReadListingAsync(marion, SeedData.Listings.Ladder);

        detail.Title.ShouldBe("Extending ladder, three sections, fibreglass");
    }

    // L2-036 AC2: Given a listing, when a member attempts to change its kind, then the response is
    // 400 Bad Request and the kind is unchanged.
    [Fact]
    public async Task A_kind_cannot_be_changed()
    {
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        var response = await marion.PutRawJsonAsync(
            $"/listings/{SeedData.Listings.Ladder}",
            """
            {
              "title": "Extending ladder, three sections",
              "description": "Sound, both spreaders lock.",
              "category": "Tools",
              "neighbourhood": "Riverdale",
              "kind": "Sell"
            }
            """,
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        // Named, so the member is told what they may not do rather than having it silently
        // ignored. A listing's kind decides its fields, its request form and its close-out verb;
        // changing it would leave all three describing something else.
        (await response.ReadInvalidFieldsAsync()).ShouldContain("kind");

        (await ReadListingAsync(marion, SeedData.Listings.Ladder)).Kind.ShouldBe(nameof(ListingKind.Lend));
    }

    // L2-041: only the owner may modify a listing. Stated here as well as for close-out, because
    // edit, archive, restore and delete are four more ways in.
    [Fact]
    public async Task A_member_may_not_edit_another_members_listing()
    {
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        var response = await priya.PutJsonAsync(
            $"/listings/{SeedData.Listings.Ladder}",
            ALadder(title: "Not hers to rename"),
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    // L2-038 AC1: Given an active listing, when its owner archives it, then it no longer appears
    // on the board and is returned among that member's archived listings.
    [Fact]
    public async Task An_archived_listing_leaves_the_board_and_joins_the_archive()
    {
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        var response = await marion.PostJsonAsync(
            $"/listings/{SeedData.Listings.Ladder}/archive",
            new { },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var board = await (await marion.GetAsync("/board", TestContext.Current.CancellationToken))
            .ReadAsync<BoardPageBody>();

        board.Listings.ShouldNotContain(listing => listing.ListingId == SeedData.Listings.Ladder);

        var archived = await ArchivedAsync(marion);

        archived.ShouldContain(listing => listing.ListingId == SeedData.Listings.Ladder);
    }

    // A listing is archived once. The second attempt is reported rather than ignored.
    [Fact]
    public async Task Archiving_a_listing_twice_answers_conflict()
    {
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        await ArchiveAsync(marion, SeedData.Listings.Ladder);

        var again = await marion.PostJsonAsync(
            $"/listings/{SeedData.Listings.Ladder}/archive",
            new { },
            TestContext.Current.CancellationToken);

        again.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    // L2-039 AC1: Given an archived listing, when its owner restores it, then its status becomes
    // active and it appears on the board.
    [Fact]
    public async Task A_restored_listing_returns_to_the_board()
    {
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        await ArchiveAsync(marion, SeedData.Listings.Ladder);

        var response = await marion.PostJsonAsync(
            $"/listings/{SeedData.Listings.Ladder}/restore",
            new { },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        (await StatusOfAsync(SeedData.Listings.Ladder)).ShouldBe(ListingStatus.Active);

        var board = await (await marion.GetAsync("/board", TestContext.Current.CancellationToken))
            .ReadAsync<BoardPageBody>();

        board.Listings.ShouldContain(listing => listing.ListingId == SeedData.Listings.Ladder);
    }

    // The rule L2-039 and L2-037 would otherwise disagree about.
    //
    // Closing out a Lend listing leaves it Archived, exactly as shelving one does, so the status
    // alone cannot say whether a listing may come back. A returned ladder has served its purpose
    // and restoring it would be reopening a finished loan; a shelved one has not. ClosedOutAt is
    // what tells them apart.
    [Fact]
    public async Task A_listing_that_was_closed_out_cannot_be_restored()
    {
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        var closeOut = await marion.PostJsonAsync(
            $"/listings/{SeedData.Listings.Ladder}/close-out",
            new { },
            TestContext.Current.CancellationToken);

        closeOut.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Archived either way, which is precisely why the status is not the discriminator.
        (await StatusOfAsync(SeedData.Listings.Ladder)).ShouldBe(ListingStatus.Archived);

        var restore = await marion.PostJsonAsync(
            $"/listings/{SeedData.Listings.Ladder}/restore",
            new { },
            TestContext.Current.CancellationToken);

        restore.StatusCode.ShouldBe(HttpStatusCode.Conflict);

        (await StatusOfAsync(SeedData.Listings.Ladder)).ShouldBe(ListingStatus.Archived);
    }

    // L2-040 AC1: Given an archived listing, when its owner deletes it, then a subsequent request
    // for it returns 404 Not Found.
    [Fact]
    public async Task A_deleted_listing_is_gone_for_good()
    {
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        await ArchiveAsync(marion, SeedData.Listings.Ladder);

        var response = await marion.DeleteAsync(
            $"/listings/{SeedData.Listings.Ladder}",
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var reread = await marion.GetAsync(
            $"/listings/{SeedData.Listings.Ladder}",
            TestContext.Current.CancellationToken);

        reread.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        (await CountListingsAsync(SeedData.Listings.Ladder)).ShouldBe(0);
    }

    // Deleting takes the conversations with it. A thread whose listing has gone leads nowhere,
    // and L2-064 requires every thread to carry its listing.
    [Fact]
    public async Task Deleting_a_listing_takes_its_requests_and_threads_with_it()
    {
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        var borrow = await priya.PostJsonAsync(
            $"/listings/{SeedData.Listings.Ladder}/requests/loan",
            Fixtures.Requests.ToBorrow(),
            TestContext.Current.CancellationToken);

        borrow.StatusCode.ShouldBe(HttpStatusCode.Created);

        var made = await borrow.ReadAsync<MadeRequest>();

        var accepted = await marion.PostJsonAsync(
            $"/requests/{made.RequestId}/accept",
            new { },
            TestContext.Current.CancellationToken);

        accepted.StatusCode.ShouldBe(HttpStatusCode.OK);

        await ArchiveAsync(marion, SeedData.Listings.Ladder);

        var deleted = await marion.DeleteAsync(
            $"/listings/{SeedData.Listings.Ladder}",
            TestContext.Current.CancellationToken);

        deleted.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var threads = await (await priya.GetAsync("/threads", TestContext.Current.CancellationToken))
            .ReadAsync<IReadOnlyList<ThreadSummaryBody>>();

        threads.ShouldNotContain(thread => thread.ListingId == SeedData.Listings.Ladder);

        var mine = await (await priya.GetAsync("/requests/mine", TestContext.Current.CancellationToken))
            .ReadAsync<IReadOnlyList<MyRequestBody>>();

        mine.ShouldNotContain(request => request.ListingId == SeedData.Listings.Ladder);
    }

    // An active listing is not deleted out from under the board. Archive first, so a member has a
    // moment between taking it down and destroying it.
    [Fact]
    public async Task An_active_listing_cannot_be_deleted()
    {
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        var response = await marion.DeleteAsync(
            $"/listings/{SeedData.Listings.Ladder}",
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);

        (await CountListingsAsync(SeedData.Listings.Ladder)).ShouldBe(1);
    }

    private static object ALadder(string? title = null) => new
    {
        title = title ?? "Extending ladder, three sections",
        description = "Sound, both spreaders lock. Kept in the garage.",
        category = "Tools",
        neighbourhood = SeedData.Marion.Neighbourhood,
    };

    private static async Task ArchiveAsync(HttpClient client, Guid listingId)
    {
        var response = await client.PostJsonAsync(
            $"/listings/{listingId}/archive",
            new { },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private static async Task<ListingDetailBody> ReadListingAsync(HttpClient client, Guid listingId) =>
        await (await client.GetAsync($"/listings/{listingId}", TestContext.Current.CancellationToken))
            .ReadAsync<ListingDetailBody>();

    private static async Task<IReadOnlyList<MyListingBody>> ArchivedAsync(HttpClient client)
    {
        var mine = await (await client.GetAsync(
                "/listings/mine?includeClosed=true",
                TestContext.Current.CancellationToken))
            .ReadAsync<IReadOnlyList<MyListingBody>>();

        return [.. mine.Where(listing => listing.Status != nameof(ListingStatus.Active))];
    }

    private Task<ListingStatus> StatusOfAsync(Guid listingId) =>
        Api.QueryAsync(context => context
            .Set<Listing>()
            .IgnoreQueryFilters()
            .Where(listing => listing.Id == listingId)
            .Select(listing => listing.Status)
            .SingleAsync());

    private Task<int> CountListingsAsync(Guid listingId) =>
        Api.QueryAsync(context => context
            .Set<Listing>()
            .IgnoreQueryFilters()
            .CountAsync(listing => listing.Id == listingId));
}
