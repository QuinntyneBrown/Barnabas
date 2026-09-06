using System.Net;
using Barnabas.Domain.Listings;
using Barnabas.IntegrationTests.Fixtures;
using Barnabas.Infrastructure.Persistence.Seeding;

namespace Barnabas.IntegrationTests.Board;

// Acceptance Test
// Traces to: L2-042, L2-043, L2-044, L2-045
// Description: The board shows the congregation's active listings with what a placard renders,
// and carries each listing's kind as data rather than only as a colour.
public sealed class BrowseTheBoardTests : AcceptanceTest
{
    public BrowseTheBoardTests(BarnabasApiFactory api)
        : base(api)
    {
    }

    // L2-042 AC1: Given a congregation with active listings, when a member requests the board,
    // then each is returned with kind, title, owner display name, and neighbourhood.
    [Fact]
    public async Task The_board_carries_what_a_placard_renders()
    {
        using var client = await Api.ClientForAsync(SeedData.Priya.Id);

        var response = await client.GetAsync("/board", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var page = await response.ReadAsync<BoardPageBody>();

        page.Listings.Count.ShouldBe(2);

        var ladder = page.Listings.Single(listing => listing.ListingId == SeedData.Listings.Ladder);

        ladder.Kind.ShouldBe(nameof(ListingKind.Lend));
        ladder.Title.ShouldBe(SeedData.Listings.LadderTitle);
        ladder.OwnerDisplayName.ShouldBe(SeedData.Marion.DisplayName);
        ladder.Neighbourhood.ShouldBe(SeedData.Marion.Neighbourhood);

        page.Listings.Single(listing => listing.ListingId == SeedData.Listings.Drill).Price.ShouldBe(45.00m);
    }

    // L2-042 AC2: Given a congregation with an archived listing, when a member requests the
    // board, then that listing is absent.
    [Fact]
    public async Task A_closed_out_listing_is_absent_from_the_board()
    {
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        await marion.PostJsonAsync(
            $"/listings/{SeedData.Listings.Drill}/close-out",
            new { },
            TestContext.Current.CancellationToken);

        var page = await (await marion.GetAsync("/board", TestContext.Current.CancellationToken))
            .ReadAsync<BoardPageBody>();

        page.Listings.ShouldNotContain(listing => listing.ListingId == SeedData.Listings.Drill);
        page.Counts.ShouldNotContainKey(nameof(ListingKind.Sell));
    }

    // L2-045 AC1: Given a congregation with no active listings, when a member requests the board,
    // then an empty collection is returned with a 200 status.
    //
    // An empty board is an ordinary state rather than an error. A congregation that has posted
    // nothing gets 200 and nothing in it, so the screen can invite the first listing instead of
    // reporting a fault.
    [Fact]
    public async Task An_empty_board_is_an_empty_collection_and_not_a_failure()
    {
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        foreach (var listingId in new[] { SeedData.Listings.Ladder, SeedData.Listings.Drill })
        {
            var archived = await marion.PostJsonAsync(
                $"/listings/{listingId}/archive",
                new { },
                TestContext.Current.CancellationToken);

            archived.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        var response = await marion.GetAsync("/board", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var page = await response.ReadAsync<BoardPageBody>();

        page.Listings.ShouldBeEmpty();
        page.NextCursor.ShouldBeNull();
    }

    // L2-043 AC1: Given a board of mixed kinds, when it is requested filtered to Lend, then only
    // Lend listings are returned.
    //
    // The filter is a predicate on the query rather than a screen hiding placards, which is what
    // lets a filtered board page correctly and what makes this assertable at all.
    [Fact]
    public async Task The_board_filtered_to_a_kind_returns_only_that_kind()
    {
        using var client = await Api.ClientForAsync(SeedData.Marion.Id);

        var everything = await (await client.GetAsync("/board", TestContext.Current.CancellationToken))
            .ReadAsync<BoardPageBody>();

        // The seed holds more than one kind, or this would prove nothing.
        everything.Listings.Select(listing => listing.Kind).Distinct().Count().ShouldBeGreaterThan(1);

        var lendOnly = await (await client.GetAsync("/board?kind=Lend", TestContext.Current.CancellationToken))
            .ReadAsync<BoardPageBody>();

        lendOnly.Listings.ShouldNotBeEmpty();
        lendOnly.Listings.ShouldAllBe(listing => listing.Kind == nameof(ListingKind.Lend));

        // The counts still describe the whole board, because they label the filters rather than
        // the page. A count that shrank with the filter would tell a member the other kinds had
        // gone.
        lendOnly.Counts.ShouldBe(everything.Counts);
    }

    // L2-044 AC1: Given any listing on the board, when it is rendered, then its kind is present
    // as text within the listing.
    //
    // The screen half of this is a Playwright test. Its API half is that the kind travels as its
    // own name rather than as a number the screen would have to translate into a colour.
    [Fact]
    public async Task Every_listing_carries_its_kind_as_a_name()
    {
        using var client = await Api.ClientForAsync(SeedData.Priya.Id);

        var page = await (await client.GetAsync("/board", TestContext.Current.CancellationToken))
            .ReadAsync<BoardPageBody>();

        page.Listings.ShouldAllBe(listing => listing.Kind.Length > 0);
        page.Listings.Select(listing => listing.Kind)
            .ShouldBeSubsetOf([
                nameof(ListingKind.Lend),
                nameof(ListingKind.Give),
                nameof(ListingKind.Sell),
                nameof(ListingKind.Help),
            ]);
    }

    // The counts label the filter chips, so they describe the whole board rather than the page.
    [Fact]
    public async Task The_board_reports_a_count_for_each_kind_present()
    {
        using var client = await Api.ClientForAsync(SeedData.Priya.Id);

        var page = await (await client.GetAsync("/board", TestContext.Current.CancellationToken))
            .ReadAsync<BoardPageBody>();

        page.Counts[nameof(ListingKind.Lend)].ShouldBe(1);
        page.Counts[nameof(ListingKind.Sell)].ShouldBe(1);
    }

    // A parish board is small, but the shape is settled now rather than when a large
    // congregation joins.
    [Fact]
    public async Task The_board_pages_by_cursor()
    {
        using var client = await Api.ClientForAsync(SeedData.Priya.Id);

        var first = await (await client.GetAsync("/board?limit=1", TestContext.Current.CancellationToken))
            .ReadAsync<BoardPageBody>();

        first.Listings.Count.ShouldBe(1);
        first.NextCursor.ShouldNotBeNull();

        var second = await (await client.GetAsync(
            $"/board?limit=1&cursor={Uri.EscapeDataString(first.NextCursor)}",
            TestContext.Current.CancellationToken)).ReadAsync<BoardPageBody>();

        second.Listings.Count.ShouldBe(1);
        second.Listings[0].ListingId.ShouldNotBe(first.Listings[0].ListingId);
        second.NextCursor.ShouldBeNull();
    }
}
