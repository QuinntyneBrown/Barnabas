using System.Net;
using Barnabas.Domain.Listings;
using Barnabas.Domain.Requests;
using Barnabas.IntegrationTests.Fixtures;
using Barnabas.Infrastructure.Persistence.Seeding;

namespace Barnabas.IntegrationTests;

// Acceptance Test
// Traces to: L2-012, L2-014, L2-027, L2-042, L2-054, L2-060, L2-065, L2-066, L2-067, L2-037
// Description: The whole of feature slice 1 in one pass - sign in, post a Lend listing, see it on
// the board, ask to borrow it, accept, arrange the handoff in the thread that opens, and mark it
// taken so it leaves the board.
//
// The per-requirement tests each say one thing precisely. This one says that the things join up,
// which none of them can: it is the path the slice exists to make possible, and it is the
// definition of done for the slice stated as a test.
public sealed class BorrowALadderTests : AcceptanceTest
{
    public BorrowALadderTests(BarnabasApiFactory api)
        : base(api)
    {
    }

    [Fact]
    public async Task A_member_borrows_a_ladder_from_another_member()
    {
        // Sign in, both of them, by following a real link.
        var owner = await Device.SignInAsync(Api, SeedData.Marion.EmailAddress);
        var borrower = await Device.SignInAsync(Api, SeedData.Priya.EmailAddress);

        // Post a Lend listing.
        var posted = await owner.PostAsync("/listings/lend", new
        {
            title = "Extending ladder, three sections",
            description = "Sound, both spreaders lock. Kept in the garage.",
            category = "Tools",
            neighbourhood = SeedData.Marion.Neighbourhood,
            returnBy = "2026-10-01",
        });

        posted.StatusCode.ShouldBe(HttpStatusCode.Created, Api);

        var listingId = (await posted.ReadAsync<PostedListing>()).ListingId;

        // See it on the board.
        var board = await (await borrower.GetAsync("/board")).ReadAsync<BoardPageBody>();

        var placard = board.Listings.Single(listing => listing.ListingId == listingId);

        placard.Kind.ShouldBe(nameof(ListingKind.Lend));
        placard.OwnerDisplayName.ShouldBe(SeedData.Marion.DisplayName);

        // A second member asks to borrow it.
        var asked = await borrower.PostAsync(
            $"/listings/{listingId}/requests/loan",
            Fixtures.Requests.ToBorrow("Painting the back bedroom the weekend after next."));

        asked.StatusCode.ShouldBe(HttpStatusCode.Created, Api);

        var requestId = (await asked.ReadAsync<MadeRequest>()).RequestId;

        // The owner sees it waiting.
        var incoming = await (await owner.GetAsync("/requests/incoming"))
            .ReadAsync<IReadOnlyList<IncomingRequestBody>>();

        incoming.ShouldHaveSingleItem().RequestId.ShouldBe(requestId);

        // The owner accepts, and a thread opens.
        var decision = await owner.PostAsync($"/requests/{requestId}/accept", new { });

        decision.StatusCode.ShouldBe(HttpStatusCode.OK, Api);

        var threadId = (await decision.ReadAsync<AcceptedRequest>()).ThreadId;

        // Both members exchange a message.
        var fromOwner = await owner.PostAsync(
            $"/threads/{threadId}/messages",
            new { body = "Thursday after choir suits me. It will be by the side door." });

        fromOwner.StatusCode.ShouldBe(HttpStatusCode.Created, Api);

        Api.Clock.Advance(TimeSpan.FromMinutes(1));

        var fromBorrower = await borrower.PostAsync(
            $"/threads/{threadId}/messages",
            new { body = "Thursday it is. Thank you, Marion." });

        fromBorrower.StatusCode.ShouldBe(HttpStatusCode.Created, Api);

        var conversation = await (await borrower.GetAsync($"/threads/{threadId}"))
            .ReadAsync<ThreadDetailBody>();

        conversation.Messages.Count.ShouldBe(2);
        conversation.RequestStatus.ShouldBe(nameof(RequestStatus.Accepted));
        conversation.ListingTitle.ShouldBe("Extending ladder, three sections");

        // The owner marks it taken.
        var closed = await owner.PostAsync($"/listings/{listingId}/close-out", new { });

        closed.StatusCode.ShouldBe(HttpStatusCode.OK, Api);
        (await closed.ReadAsync<ClosedOutListing>()).Status.ShouldBe(nameof(ListingStatus.Archived));

        // And it leaves the board.
        var afterwards = await (await borrower.GetAsync("/board")).ReadAsync<BoardPageBody>();

        afterwards.Listings.ShouldNotContain(listing => listing.ListingId == listingId);

        // The thread it was arranged in outlives the listing coming off the board: the two
        // members still have to actually meet.
        var threads = await (await borrower.GetAsync("/threads"))
            .ReadAsync<IReadOnlyList<ThreadSummaryBody>>();

        threads.ShouldHaveSingleItem().ThreadId.ShouldBe(threadId);
    }
}
