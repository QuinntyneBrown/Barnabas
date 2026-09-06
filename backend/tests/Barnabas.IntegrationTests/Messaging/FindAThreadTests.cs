using System.Net;
using Barnabas.IntegrationTests.Fixtures;
using Barnabas.Infrastructure.Persistence.Seeding;

namespace Barnabas.IntegrationTests.Messaging;

// Acceptance Test
// Traces to: L2-065
// Description: A member sees the threads they are party to, each showing the other member, the
// listing, the latest message, and whether it is unread - and sees no others.
public sealed class FindAThreadTests : AcceptanceTest
{
    public FindAThreadTests(BarnabasApiFactory api)
        : base(api)
    {
    }

    // L2-065 AC1: Given a member with threads, when they list them, then each is returned with
    // the other member, the listing, the latest message, and an unread flag.
    [Fact]
    public async Task A_thread_shows_who_it_is_with_what_it_is_about_and_the_last_thing_said()
    {
        var threadId = await Handoff.AboutTheLadderAsync(Api);

        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        await marion.SayAsync(threadId, "It is by the side door whenever suits.");

        var threads = await priya.ThreadsAsync();

        var only = threads.ShouldHaveSingleItem();

        only.ThreadId.ShouldBe(threadId);
        only.OtherMemberId.ShouldBe(SeedData.Marion.Id);
        only.OtherMemberDisplayName.ShouldBe(SeedData.Marion.DisplayName);
        only.ListingId.ShouldBe(SeedData.Listings.Ladder);
        only.ListingTitle.ShouldBe(SeedData.Listings.LadderTitle);
        only.LatestMessage.ShouldBe("It is by the side door whenever suits.");
        only.Unread.ShouldBeTrue();
    }

    // The other member is whoever the reader is not, so the same thread reads differently from
    // each side.
    [Fact]
    public async Task Each_party_sees_the_other_one()
    {
        await Handoff.AboutTheLadderAsync(Api);

        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        (await marion.ThreadsAsync()).Single().OtherMemberId.ShouldBe(SeedData.Priya.Id);
        (await priya.ThreadsAsync()).Single().OtherMemberId.ShouldBe(SeedData.Marion.Id);
    }

    // L2-065 AC2: Given a member, when they list threads, then threads to which they are not a
    // party are absent.
    [Fact]
    public async Task A_member_sees_no_thread_they_are_not_part_of()
    {
        await Handoff.AboutTheLadderAsync(Api);

        using var grace = await Api.ClientForAsync(SeedData.Grace.Id);

        (await grace.ThreadsAsync()).ShouldBeEmpty();
    }

    // A thread with nothing said in it yet is still a thread. It opened the moment the request
    // was accepted, and the member has to be able to find it in order to say the first thing.
    [Fact]
    public async Task A_thread_with_nothing_said_in_it_is_still_listed()
    {
        var threadId = await Handoff.AboutTheLadderAsync(Api);

        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        var only = (await priya.ThreadsAsync()).ShouldHaveSingleItem();

        only.ThreadId.ShouldBe(threadId);
        only.LatestMessage.ShouldBeEmpty();
        only.LatestAt.ShouldBeNull();
        only.Unread.ShouldBeFalse();
    }

    // There is no way to start a conversation any other way, and no endpoint that would.
    [Fact]
    public async Task There_is_no_endpoint_that_creates_a_thread()
    {
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        var response = await priya.PostJsonAsync(
            "/threads",
            new { listingId = SeedData.Listings.Ladder },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBeOneOf(HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed);
    }
}
