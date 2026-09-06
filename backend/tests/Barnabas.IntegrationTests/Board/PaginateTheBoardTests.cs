using Barnabas.Domain.Listings;
using Barnabas.IntegrationTests.Fixtures;
using Barnabas.Infrastructure.Persistence.Seeding;

namespace Barnabas.IntegrationTests.Board;

// Acceptance Test
// Traces to: L2-105
// Description: A congregation large enough to outgrow one screen gets its board in bounded pages,
// each carrying a cursor for the next. A page size beyond the documented maximum is brought down
// to it rather than refused.
public sealed class PaginateTheBoardTests : AcceptanceTest
{
    private const int ACongregationsWorth = 500;

    public PaginateTheBoardTests(BarnabasApiFactory api)
        : base(api)
    {
    }

    // L2-105 AC1: Given a congregation with 500 listings, when the board is requested without a
    // page parameter, then at most 50 are returned with a cursor for the next page.
    [Fact]
    public async Task A_board_of_five_hundred_arrives_in_a_bounded_first_page()
    {
        await ABoardOfAsync(ACongregationsWorth);

        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        var page = await (await marion.GetAsync("/board", TestContext.Current.CancellationToken))
            .ReadAsync<BoardPageBody>();

        page.Listings.Count.ShouldBeLessThanOrEqualTo(50);
        page.NextCursor.ShouldNotBeNull();
    }

    // L2-105 AC2: Given a cursor, when the next page is requested, then the following listings are
    // returned without repeating any from the previous page.
    [Fact]
    public async Task A_cursor_carries_on_where_the_page_before_it_stopped()
    {
        await ABoardOfAsync(ACongregationsWorth);

        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        var first = await (await marion.GetAsync("/board", TestContext.Current.CancellationToken))
            .ReadAsync<BoardPageBody>();

        var second = await (await marion.GetAsync(
                $"/board?cursor={Uri.EscapeDataString(first.NextCursor!)}",
                TestContext.Current.CancellationToken))
            .ReadAsync<BoardPageBody>();

        second.Listings.ShouldNotBeEmpty();

        var seen = first.Listings.Select(listing => listing.ListingId).ToHashSet();

        second.Listings.ShouldAllBe(listing => !seen.Contains(listing.ListingId));
    }

    // Paging all the way through reaches the end and stops, rather than cycling.
    [Fact]
    public async Task Paging_to_the_end_returns_every_listing_once()
    {
        await ABoardOfAsync(120);

        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        var seen = new HashSet<Guid>();
        string? cursor = null;
        var pages = 0;
        var onTheBoard = 0;

        do
        {
            var route = cursor is null ? "/board?limit=50" : $"/board?limit=50&cursor={Uri.EscapeDataString(cursor)}";

            var page = await (await marion.GetAsync(route, TestContext.Current.CancellationToken))
                .ReadAsync<BoardPageBody>();

            // The counts describe the whole board rather than the page, so the first page already
            // knows how many there are to find. Derived rather than written down, because the
            // seed's own size is not what this test is about.
            onTheBoard = page.Counts.Values.Sum();

            foreach (var listing in page.Listings)
            {
                seen.Add(listing.ListingId).ShouldBeTrue("no listing appears on two pages");
            }

            cursor = page.NextCursor;
            pages += 1;

            pages.ShouldBeLessThan(20, "paging terminates rather than cycling");
        }
        while (cursor is not null);

        seen.Count.ShouldBe(onTheBoard);
        seen.Count.ShouldBeGreaterThan(120);
    }

    // L2-105 AC3: Given a page size above the documented maximum, when it is requested, then the
    // maximum is applied rather than the requested value.
    //
    // Clamped, not refused. L2-096 requires an over-long *field* to be rejected naming it, but a
    // page size is a transport hint rather than something a member typed - answering 400 would
    // make a caller asking for too much get nothing at all.
    [Fact]
    public async Task A_page_size_beyond_the_maximum_is_brought_down_to_it()
    {
        await ABoardOfAsync(ACongregationsWorth);

        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        var response = await marion.GetAsync("/board?limit=500", TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();

        var page = await response.ReadAsync<BoardPageBody>();

        page.Listings.Count.ShouldBe(50);
    }

    /// <summary>Fills St. Aidan's board, so paging has something to page through.</summary>
    private Task ABoardOfAsync(int count) =>
        Api.ArrangeAsync(async context =>
        {
            var postedAt = new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.Zero);

            for (var index = 0; index < count; index += 1)
            {
                context.Add(Listing.PostLend(
                    Guid.NewGuid(),
                    SeedData.StAidans.Id,
                    SeedData.Marion.Id,
                    $"Listing {index:D4}",
                    "One of many, so the board outgrows a single screen.",
                    "Tools",
                    SeedData.Marion.Neighbourhood,
                    new DateOnly(2026, 12, 1),

                    // Distinct instants, so the keyset cursor has a total order to walk and the
                    // test is not asserting against a tie-break it never arranged.
                    postedAt.AddMinutes(index)));
            }

            await context.SaveChangesAsync();
        });
}
