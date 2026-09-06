using System.Net;
using Barnabas.Domain.Listings;
using Barnabas.Domain.Requests;
using Barnabas.IntegrationTests.Fixtures;
using Barnabas.Infrastructure.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;

namespace Barnabas.IntegrationTests.Listings;

// Acceptance Test
// Traces to: L2-035, L2-041, L2-094
// Description: A member sees the listings they own with their open request counts, and nobody
// else can change them.
public sealed class ManageMyListingsTests : AcceptanceTest
{
    public ManageMyListingsTests(BarnabasApiFactory api)
        : base(api)
    {
    }

    // L2-035 AC1: Given a member with three active listings, when they request their listings,
    // then all three are returned with their kind, status, and open request count.
    [Fact]
    public async Task A_member_sees_their_own_listings_with_their_kind_status_and_open_requests()
    {
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        // Marion is seeded with two; a third makes the count worth asserting.
        await marion.PostJsonAsync(
            "/listings/lend",
            new
            {
                title = "Wheelbarrow",
                description = "Pneumatic tyre, recently pumped.",
                category = "Outdoor",
                neighbourhood = SeedData.Marion.Neighbourhood,
                returnBy = "2026-10-01",
            },
            TestContext.Current.CancellationToken);

        // Arranged directly. This is a test of what an owner sees, not of the request endpoint,
        // which has acceptance tests of its own.
        await Api.ArrangeAsync(context =>
        {
            context.Add(ListingRequest.MakeLoanRequest(
                Guid.NewGuid(),
                SeedData.StAidans.Id,
                SeedData.Listings.Ladder,
                SeedData.Priya.Id,
                "Painting the back bedroom.",
                new DateOnly(2026, 9, 13),
                new DateOnly(2026, 9, 20),
                Api.Clock.GetUtcNow()));

            return Task.CompletedTask;
        });

        var response = await marion.GetAsync("/listings/mine", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var mine = await response.ReadAsync<IReadOnlyList<MyListingBody>>();

        mine.Count.ShouldBe(3);
        mine.ShouldAllBe(listing => listing.Status == nameof(ListingStatus.Active));

        var ladder = mine.Single(listing => listing.ListingId == SeedData.Listings.Ladder);

        ladder.Kind.ShouldBe(nameof(ListingKind.Lend));
        ladder.OpenRequestCount.ShouldBe(1);

        mine.Single(listing => listing.ListingId == SeedData.Listings.Drill).OpenRequestCount.ShouldBe(0);
    }

    // L2-035: another member's listings are not the caller's own.
    [Fact]
    public async Task A_member_sees_only_their_own_listings()
    {
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        var mine = await (await priya.GetAsync("/listings/mine", TestContext.Current.CancellationToken))
            .ReadAsync<IReadOnlyList<MyListingBody>>();

        mine.ShouldBeEmpty();
    }

    // L2-041 AC1, L2-094 AC1: Given a listing owned by another member of the same congregation,
    // when a member modifies it, then the response is 403 Forbidden and nothing changes.
    //
    // Close-out is the modification feature slice 1 has; edit, archive, and delete are later.
    [Fact]
    public async Task Another_member_cannot_close_out_a_listing_they_do_not_own()
    {
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        var response = await priya.PostJsonAsync(
            $"/listings/{SeedData.Listings.Ladder}/close-out",
            new { },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        (await StatusOfAsync(SeedData.Listings.Ladder)).ShouldBe(ListingStatus.Active);
    }

    // L2-041 AC3 is an E2E criterion about the screen offering no owner actions. Its API half is
    // that the detail response says plainly whose listing it is.
    [Fact]
    public async Task A_listing_says_whether_the_caller_owns_it()
    {
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        var asOwner = await (await marion.GetAsync(
            $"/listings/{SeedData.Listings.Ladder}",
            TestContext.Current.CancellationToken)).ReadAsync<ListingDetailBody>();

        var asVisitor = await (await priya.GetAsync(
            $"/listings/{SeedData.Listings.Ladder}",
            TestContext.Current.CancellationToken)).ReadAsync<ListingDetailBody>();

        asOwner.IsOwnedByCaller.ShouldBeTrue();
        asVisitor.IsOwnedByCaller.ShouldBeFalse();
        asVisitor.OwnerDisplayName.ShouldBe(SeedData.Marion.DisplayName);
    }

    private Task<ListingStatus> StatusOfAsync(Guid listingId) =>
        Api.QueryAsync(async context =>
            (await context.Set<Listing>().IgnoreQueryFilters().SingleAsync(l => l.Id == listingId)).Status);
}
