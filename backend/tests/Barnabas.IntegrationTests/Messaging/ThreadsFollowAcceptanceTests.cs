using System.Net;
using Barnabas.Domain.Messaging;
using Barnabas.IntegrationTests.Fixtures;
using Barnabas.Infrastructure.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;

namespace Barnabas.IntegrationTests.Messaging;

// Acceptance Test
// Traces to: L2-064
// Description: A thread exists as the consequence of an accepted request and in no other way.
// There is no route to start one, and a pending or declined request opens nothing.
//
// L2-060 states this from the request's side - accepting opens a thread - and is already covered
// by AcceptAndDeclineTests. These are the messaging-side assertions on the same behaviour: that
// the thread carries its listing and both members, and that nothing else can bring one into
// being. Neither requirement is implemented twice; they are two statements about one rule.
public sealed class ThreadsFollowAcceptanceTests : AcceptanceTest
{
    public ThreadsFollowAcceptanceTests(BarnabasApiFactory api)
        : base(api)
    {
    }

    // L2-064 AC1: Given a request accepted by the owner, when threads are listed for either
    // member, then a thread exists carrying the listing, the owner, and the requester.
    [Fact]
    public async Task An_accepted_request_opens_one_thread_both_members_can_see()
    {
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        var requestId = await AskAsync(priya);

        await AcceptAsync(marion, requestId);

        var hers = await ThreadsOfAsync(priya);
        var his = await ThreadsOfAsync(marion);

        var fromTheRequester = hers.ShouldHaveSingleItem();
        var fromTheOwner = his.ShouldHaveSingleItem();

        // One thread, seen from two sides, and each side is shown the other member.
        fromTheRequester.ThreadId.ShouldBe(fromTheOwner.ThreadId);
        fromTheRequester.ListingId.ShouldBe(SeedData.Listings.Ladder);
        fromTheOwner.ListingId.ShouldBe(SeedData.Listings.Ladder);

        fromTheRequester.OtherMemberId.ShouldBe(SeedData.Marion.Id);
        fromTheOwner.OtherMemberId.ShouldBe(SeedData.Priya.Id);

        // It carries the listing it concerns, so it is never a conversation about nothing.
        fromTheRequester.ListingTitle.ShouldBe(SeedData.Listings.LadderTitle);
    }

    // L2-064 AC2: Given a pending or declined request, when threads are listed, then no thread
    // exists for it.
    [Fact]
    public async Task A_pending_request_opens_no_thread()
    {
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        await AskAsync(priya);

        (await ThreadsOfAsync(priya)).ShouldBeEmpty();
        (await CountThreadsAsync()).ShouldBe(0);
    }

    [Fact]
    public async Task A_declined_request_opens_no_thread()
    {
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        var requestId = await AskAsync(priya);

        var declined = await marion.PostJsonAsync(
            $"/requests/{requestId}/decline",
            new { },
            TestContext.Current.CancellationToken);

        declined.StatusCode.ShouldBe(HttpStatusCode.OK);

        (await ThreadsOfAsync(priya)).ShouldBeEmpty();
        (await ThreadsOfAsync(marion)).ShouldBeEmpty();
        (await CountThreadsAsync()).ShouldBe(0);
    }

    // L2-064 AC3: Given the API surface, when it is enumerated, then no endpoint creates a thread
    // without an accepted request.
    //
    // Stated as behaviour rather than by reading the route table: the ways a caller could try to
    // start a conversation are attempted, and none of them opens one. A member who wants to talk
    // to another member asks for something first - that is the whole shape of the product.
    [Theory]
    [InlineData("/threads")]
    [InlineData("/messagethreads")]
    public async Task There_is_no_way_to_start_a_thread_directly(string route)
    {
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        var response = await priya.PostJsonAsync(
            route,
            new { listingId = SeedData.Listings.Ladder, otherMemberId = SeedData.Marion.Id },
            TestContext.Current.CancellationToken);

        // Not found or not allowed - either way there is nothing there to call.
        response.StatusCode.ShouldBeOneOf(HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed);

        (await CountThreadsAsync()).ShouldBe(0);
    }

    // The count of threads never exceeds the count of accepted requests, which is the invariant
    // the three criteria above are each a view of.
    [Fact]
    public async Task There_are_never_more_threads_than_accepted_requests()
    {
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);
        using var grace = await Api.ClientForAsync(SeedData.Grace.Id);

        var hers = await AskAsync(priya);
        var also = await AskForTheDrillAsync(grace);

        await AcceptAsync(marion, hers);

        var declined = await marion.PostJsonAsync(
            $"/requests/{also}/decline",
            new { },
            TestContext.Current.CancellationToken);

        declined.StatusCode.ShouldBe(HttpStatusCode.OK);

        (await CountThreadsAsync()).ShouldBe(1);
    }

    private static async Task<Guid> AskAsync(HttpClient client)
    {
        var response = await client.PostJsonAsync(
            $"/listings/{SeedData.Listings.Ladder}/requests/loan",
            Fixtures.Requests.ToBorrow(),
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        return (await response.ReadAsync<MadeRequest>()).RequestId;
    }

    private static async Task<Guid> AskForTheDrillAsync(HttpClient client)
    {
        var response = await client.PostJsonAsync(
            $"/listings/{SeedData.Listings.Drill}/requests/purchase",
            new { message = "Is it still going?", pickupAt = "2026-10-01T14:00:00+00:00" },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        return (await response.ReadAsync<MadeRequest>()).RequestId;
    }

    private static async Task AcceptAsync(HttpClient client, Guid requestId)
    {
        var response = await client.PostJsonAsync(
            $"/requests/{requestId}/accept",
            new { },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private static async Task<IReadOnlyList<ThreadSummaryBody>> ThreadsOfAsync(HttpClient client) =>
        await (await client.GetAsync("/threads", TestContext.Current.CancellationToken))
            .ReadAsync<IReadOnlyList<ThreadSummaryBody>>();

    private Task<int> CountThreadsAsync() =>
        Api.QueryAsync(context => context.Set<MessageThread>().IgnoreQueryFilters().CountAsync());
}
