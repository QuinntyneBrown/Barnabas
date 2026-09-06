using System.Net;
using Barnabas.Domain.Listings;
using Barnabas.IntegrationTests.Fixtures;
using Barnabas.Infrastructure.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;

namespace Barnabas.IntegrationTests.Listings;

// Acceptance Test
// Traces to: L2-037 (partial - AC3, the Lend branch)
// Description: An owner marks a Lend listing taken, its status becomes Archived, and it leaves
// the board.
//
// L2-037 also names Sell, Give, and Help. Those posting flows are out of feature slice 1, so
// this covers AC3 alone and the requirement stands as partially delivered. The transition itself
// is kind-aware on the entity, so the other three need no new code when their forms arrive.
public sealed class CloseOutAListingTests : AcceptanceTest
{
    public CloseOutAListingTests(BarnabasApiFactory api)
        : base(api)
    {
    }

    // L2-037 AC3: Given an active Lend listing, when its owner marks it taken, then its status
    // becomes Archived.
    [Fact]
    public async Task A_lend_listing_closes_out_as_archived_and_leaves_the_board()
    {
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        var response = await marion.PostJsonAsync(
            $"/listings/{SeedData.Listings.Ladder}/close-out",
            new { },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var closed = await response.ReadAsync<ClosedOutListing>();

        // Archived, not a generic closed state. The four outcomes stay distinguishable so that a
        // member reading last spring can see what became of each thing.
        closed.Status.ShouldBe(nameof(ListingStatus.Archived));
        (await StatusOfAsync(SeedData.Listings.Ladder)).ShouldBe(ListingStatus.Archived);

        var board = await (await marion.GetAsync("/board", TestContext.Current.CancellationToken))
            .ReadAsync<BoardPageBody>();

        board.Listings.ShouldNotContain(listing => listing.ListingId == SeedData.Listings.Ladder);
    }

    // A listing is closed out once. The second attempt is reported rather than ignored.
    [Fact]
    public async Task A_listing_already_closed_out_answers_conflict()
    {
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        await marion.PostJsonAsync(
            $"/listings/{SeedData.Listings.Ladder}/close-out",
            new { },
            TestContext.Current.CancellationToken);

        var second = await marion.PostJsonAsync(
            $"/listings/{SeedData.Listings.Ladder}/close-out",
            new { },
            TestContext.Current.CancellationToken);

        second.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    private Task<ListingStatus> StatusOfAsync(Guid listingId) =>
        Api.QueryAsync(async context =>
            (await context.Set<Listing>().IgnoreQueryFilters().SingleAsync(l => l.Id == listingId)).Status);
}
