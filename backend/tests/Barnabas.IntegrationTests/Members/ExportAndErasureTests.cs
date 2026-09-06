using System.Net;
using System.Net.Http.Headers;
using Barnabas.Domain.Listings;
using Barnabas.Domain.Members;
using Barnabas.Domain.Messaging;
using Barnabas.IntegrationTests.Fixtures;
using Barnabas.Infrastructure.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;

namespace Barnabas.IntegrationTests.Members;

// Acceptance Test
// Traces to: L2-101
// Description: A member can have everything Barnabas holds about them, and can ask to be
// forgotten. Erasure is irreversible and immediate, and it anonymises in place rather than
// deleting — the other party's thread has to stay legible after somebody leaves it.
public sealed class ExportAndErasureTests : AcceptanceTest
{
    public ExportAndErasureTests(BarnabasApiFactory api)
        : base(api)
    {
    }

    // L2-101 AC3: Given a member, when they request an export of their data, then their profile,
    // listings, requests, and messages are returned.
    [Fact]
    public async Task An_export_holds_the_profile_the_listings_the_requests_and_the_messages()
    {
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        var listingId = await PostAsync(priya, "Two folding chairs");
        var threadId = await AThreadAsync(priya, marion);

        await SendAsync(priya, threadId, "Saturday morning would suit.");

        var export = await ExportOfAsync(priya);

        export.Profile.MemberId.ShouldBe(SeedData.Priya.Id);
        export.Profile.DisplayName.ShouldBe(SeedData.Priya.DisplayName);
        export.Profile.CongregationName.ShouldBe(SeedData.StAidans.Name);

        // Their own address, which no other member's view of them carries — the point of AC1 and
        // AC3 together is that the data is withheld from others and available to them.
        export.Profile.EmailAddress.ShouldBe(SeedData.Priya.EmailAddress);

        export.Listings.ShouldContain(listing => listing.ListingId == listingId);
        export.Requests.ShouldContain(request => request.ListingId == SeedData.Listings.Ladder);
        export.Messages.ShouldContain(message => message.Body == "Saturday morning would suit.");
    }

    // The other party's words are the other party's data. Answering one member's rights by
    // handing over somebody else's messages would be a breach dressed as compliance.
    [Fact]
    public async Task An_export_holds_only_what_this_member_wrote()
    {
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        var threadId = await AThreadAsync(priya, marion);

        await SendAsync(priya, threadId, "Saturday morning would suit.");
        await SendAsync(marion, threadId, "Saturday is fine. It is by the side door.");

        var export = await ExportOfAsync(priya);

        export.Messages.ShouldContain(message => message.Body == "Saturday morning would suit.");
        export.Messages.ShouldNotContain(message => message.Body.Contains("side door", StringComparison.Ordinal));

        // Somebody else's listings are not hers either, even the one she asked about.
        export.Listings.ShouldNotContain(listing => listing.ListingId == SeedData.Listings.Ladder);
    }

    // L2-101 AC2: Given a member who leaves the congregation, when erasure is requested, then
    // their profile, listings, and messages are removed or irreversibly anonymised.
    [Fact]
    public async Task Erasure_takes_the_name_the_address_the_listings_and_the_words()
    {
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        var listingId = await PostAsync(priya, "Two folding chairs");
        var threadId = await AThreadAsync(priya, marion);

        await SendAsync(priya, threadId, "Saturday morning would suit.");

        var erased = await EraseAsync(priya);

        erased.MemberId.ShouldBe(SeedData.Priya.Id);
        erased.ListingsErased.ShouldBeGreaterThan(0);
        erased.MessagesErased.ShouldBeGreaterThan(0);

        var member = await MemberAsync(SeedData.Priya.Id);

        member.DisplayName.ShouldBe(Member.ErasedDisplayName);
        member.Status.ShouldBe(MemberStatus.Left);
        member.Description.ShouldBeNull();
        member.ReasonForJoining.ShouldBeNull();

        // Not the old address hashed. A hash of a known address is a lookup table away from being
        // the address again, and the requirement says irreversible.
        member.EmailAddress.ShouldNotContain("priya");
        member.EmailAddress.ShouldEndWith("@erased.invalid");

        var listing = await ListingAsync(listingId);

        listing.Title.ShouldBe(Listing.ErasedTitle);
        listing.Description.ShouldBeEmpty();
        listing.Status.ShouldBe(ListingStatus.Removed);

        (await BodyOfFirstMessageAsync(threadId)).ShouldBe(Message.ErasedBody);
    }

    // L2-066 AC1 is why this is anonymised in place and not deleted: the surviving party's thread
    // has to stay legible. A conversation with half its turns missing reads as a fault.
    [Fact]
    public async Task The_other_party_keeps_a_readable_thread()
    {
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        var threadId = await AThreadAsync(priya, marion);

        await SendAsync(priya, threadId, "Saturday morning would suit.");
        await SendAsync(marion, threadId, "Saturday is fine. It is by the side door.");

        await EraseAsync(priya);

        var thread = await (await marion.GetAsync(
            $"/threads/{threadId}",
            TestContext.Current.CancellationToken)).ReadAsync<ThreadDetailBody>();

        // Both turns still there, in order, and Marion's own words untouched.
        thread.Messages.Count.ShouldBe(2);
        thread.Messages[1].Body.ShouldBe("Saturday is fine. It is by the side door.");

        // Priya's are gone, and the thread says so rather than showing a gap.
        thread.Messages[0].Body.ShouldBe(Message.ErasedBody);
        thread.OtherMemberDisplayName.ShouldBe(Member.ErasedDisplayName);
    }

    // Erasure reaches the phone in a pocket as well as the browser that asked. Revoking only the
    // calling session would leave somebody signed in as a person who no longer has a name.
    [Fact]
    public async Task Every_session_the_member_held_ends()
    {
        using var laptop = await Api.ClientForAsync(SeedData.Priya.Id);
        using var phone = await Api.ClientForAsync(SeedData.Priya.Id);

        (await phone.GetAsync("/board", TestContext.Current.CancellationToken))
            .StatusCode.ShouldBe(HttpStatusCode.OK);

        await EraseAsync(laptop);

        (await phone.GetAsync("/board", TestContext.Current.CancellationToken))
            .StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // Their name in somebody else's notification list is exactly what they asked to have removed.
    [Fact]
    public async Task A_notification_naming_them_forgets_who_it_was_about()
    {
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        await AskToBorrowAsync(priya);

        (await NotificationsOfAsync(marion))
            .ShouldContain(notification => notification.SubjectMemberDisplayName == SeedData.Priya.DisplayName);

        await EraseAsync(priya);

        var hers = await NotificationsOfAsync(marion);

        hers.ShouldNotContain(notification => notification.SubjectMemberDisplayName == SeedData.Priya.DisplayName);
        hers.ShouldAllBe(notification => notification.SubjectMemberId == null);
    }

    // The bytes go for real. A photograph is the one part of a member's data that nothing points
    // at and that has no reason to survive them.
    [Fact]
    public async Task The_photographs_are_deleted_rather_than_anonymised()
    {
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        var listingId = await PostAsync(priya, "Two folding chairs");

        using var content = new MultipartFormDataContent();
        using var file = new ByteArrayContent(Images.Jpeg(60_000));

        file.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(file, "file", "chairs.jpg");

        var attached = await priya.PostAsync(
            $"/listings/{listingId}/photo",
            content,
            TestContext.Current.CancellationToken);

        attached.StatusCode.ShouldBe(HttpStatusCode.Created);

        var url = (await attached.ReadAsync<AttachedPhoto>()).Url;

        (await priya.GetAsync(url, TestContext.Current.CancellationToken))
            .StatusCode.ShouldBe(HttpStatusCode.OK);

        await EraseAsync(priya);

        using var grace = await Api.ClientForAsync(SeedData.Grace.Id);

        (await grace.GetAsync(url, TestContext.Current.CancellationToken))
            .StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // Somebody still waiting on a moderator is the person most likely to want to know what is
    // held about them, and to want it gone.
    [Fact]
    public async Task Somebody_awaiting_approval_may_export_and_erase()
    {
        var applicantId = Guid.NewGuid();

        await Api.ArrangeAsync(context =>
        {
            context.Add(Member.Join(
                applicantId,
                SeedData.StAidans.Id,
                "waiting@example.com",
                "Waiting W.",
                SeedData.StAidans.Neighbourhoods[0],
                "New to the parish."));

            return Task.CompletedTask;
        });

        using var applicant = await Api.ClientForAsync(applicantId);

        // The board is still refused - being able to ask what is held about you is not being let in.
        (await applicant.GetAsync("/board", TestContext.Current.CancellationToken))
            .StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        var export = await ExportOfAsync(applicant);

        export.Profile.ReasonForJoining.ShouldBe("New to the parish.");

        await EraseAsync(applicant);

        (await MemberAsync(applicantId)).DisplayName.ShouldBe(Member.ErasedDisplayName);
    }

    private static async Task<Guid> PostAsync(HttpClient client, string title)
    {
        var response = await client.PostJsonAsync(
            "/listings/give",
            new
            {
                title,
                description = "Sound, and surplus to requirements.",
                category = "Furniture",
                neighbourhood = SeedData.Priya.Neighbourhood,
            },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        return (await response.ReadAsync<PostedListing>()).ListingId;
    }

    private static async Task<Guid> AskToBorrowAsync(HttpClient client)
    {
        var response = await client.PostJsonAsync(
            $"/listings/{SeedData.Listings.Ladder}/requests/loan",
            Fixtures.Requests.ToBorrow(),
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        return (await response.ReadAsync<MadeRequest>()).RequestId;
    }

    private static async Task<Guid> AThreadAsync(HttpClient requester, HttpClient owner)
    {
        var requestId = await AskToBorrowAsync(requester);

        var accepted = await owner.PostJsonAsync(
            $"/requests/{requestId}/accept",
            new { },
            TestContext.Current.CancellationToken);

        accepted.StatusCode.ShouldBe(HttpStatusCode.OK);

        return (await accepted.ReadAsync<AcceptedRequest>()).ThreadId;
    }

    private static async Task SendAsync(HttpClient client, Guid threadId, string body)
    {
        var response = await client.PostJsonAsync(
            $"/threads/{threadId}/messages",
            new { body },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    private static async Task<MyDataExportBody> ExportOfAsync(HttpClient client)
    {
        var response = await client.GetAsync("/members/me/export", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        return await response.ReadAsync<MyDataExportBody>();
    }

    private static async Task<ErasedData> EraseAsync(HttpClient client)
    {
        var response = await client.PostJsonAsync(
            "/members/me/erase",
            new { },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        return await response.ReadAsync<ErasedData>();
    }

    private static async Task<IReadOnlyList<NotificationBody>> NotificationsOfAsync(HttpClient client) =>
        await (await client.GetAsync("/notifications", TestContext.Current.CancellationToken))
            .ReadAsync<IReadOnlyList<NotificationBody>>();

    private Task<Member> MemberAsync(Guid memberId) =>
        Api.QueryAsync(context => context
            .Set<Member>()
            .IgnoreQueryFilters()
            .SingleAsync(member => member.Id == memberId));

    private Task<Listing> ListingAsync(Guid listingId) =>
        Api.QueryAsync(context => context
            .Set<Listing>()
            .IgnoreQueryFilters()
            .SingleAsync(listing => listing.Id == listingId));

    private Task<string> BodyOfFirstMessageAsync(Guid threadId) =>
        Api.QueryAsync(context => context
            .Set<Message>()
            .IgnoreQueryFilters()
            .Where(message => message.ThreadId == threadId)
            .OrderBy(message => message.SentAt)
            .Select(message => message.Body)
            .FirstAsync());
}
