using System.Net;
using Barnabas.Domain.Members;
using Barnabas.IntegrationTests.Fixtures;
using Barnabas.Infrastructure.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;

namespace Barnabas.IntegrationTests.Moderation;

// Acceptance Test
// Traces to: L2-085, L2-086, L2-087
// Description: A moderator sees who is waiting, with the reason each gave for joining, and lets
// them in or does not. An approved member reaches the board on their next request; a declined one
// still cannot.
public sealed class ApproveAndDeclineMembersTests : AcceptanceTest
{
    public ApproveAndDeclineMembersTests(BarnabasApiFactory api)
        : base(api)
    {
    }

    // L2-085 AC1: Given three members in the pending state, when a moderator requests the queue,
    // then all three are returned with their name, neighbourhood, and stated reason for joining.
    [Fact]
    public async Task The_queue_names_everybody_waiting_and_why_they_asked()
    {
        await AnApplicantAsync("first@example.com", "Aidan F.", "Choir, and I have a van.");
        await AnApplicantAsync("second@example.com", "Bernice S.", "My neighbour suggested it.");
        await AnApplicantAsync("third@example.com", "Cormac T.", "Just moved to Riverdale.");

        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        var waiting = await PendingAsync(marion);

        waiting.Count.ShouldBe(3);

        var bernice = waiting.Single(member => member.DisplayName == "Bernice S.");

        bernice.Neighbourhood.ShouldBe(SeedData.StAidans.Neighbourhoods[0]);
        bernice.ReasonForJoining.ShouldBe("My neighbour suggested it.");
    }

    // Only a moderator, for the same reason the listing queue is a moderator's. The pending queue
    // carries what somebody wrote expecting moderators to read it.
    [Fact]
    public async Task An_ordinary_member_may_not_read_the_pending_queue()
    {
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        var response = await priya.GetAsync("/moderation/members", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    // L2-086 AC1: Given a member in the pending state, when a moderator approves them, then their
    // state becomes approved and they can request the board.
    // L2-086 AC2: Given an approved member, when they request the board, then the response is 200
    // rather than 403.
    [Fact]
    public async Task An_approved_applicant_reaches_the_board()
    {
        var applicantId = await AnApplicantAsync("newcomer@example.com", "Newcomer N.", "Keen to help.");

        using var applicant = await Api.ClientForAsync(applicantId);

        // Signed in while still waiting: the board is refused, and the screen behind that 403 is
        // the awaiting-approval one.
        (await BoardStatusAsync(applicant)).ShouldBe(HttpStatusCode.Forbidden);

        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        var decided = await DecideAsync(marion, applicantId, "approve");

        decided.Status.ShouldBe(nameof(MemberStatus.Approved));

        // The same session, without signing in again. Role and status are restamped from the
        // member record on every request, so a moderator's decision takes effect on the next
        // visit rather than the next sign-in - L2-086 AC3.
        (await BoardStatusAsync(applicant)).ShouldBe(HttpStatusCode.OK);

        // And they leave the queue, so no moderator approves the same person twice.
        (await PendingAsync(marion)).ShouldNotContain(member => member.MemberId == applicantId);
    }

    // L2-087 AC1: Given a member in the pending state, when a moderator declines them, then they
    // cannot request the board and the response to that request is 403 Forbidden.
    [Fact]
    public async Task A_declined_applicant_still_cannot_reach_the_board()
    {
        var applicantId = await AnApplicantAsync("turnedaway@example.com", "Rowan D.", "Passing through.");

        using var applicant = await Api.ClientForAsync(applicantId);
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        var decided = await DecideAsync(marion, applicantId, "decline");

        decided.Status.ShouldBe(nameof(MemberStatus.Declined));

        (await BoardStatusAsync(applicant)).ShouldBe(HttpStatusCode.Forbidden);

        // Kept rather than deleted, and out of the queue: a declined applicant is not waiting on
        // anything, and leaving them there would mean deciding about them every week.
        (await StatusOfAsync(applicantId)).ShouldBe(MemberStatus.Declined);
        (await PendingAsync(marion)).ShouldNotContain(member => member.MemberId == applicantId);
    }

    [Fact]
    public async Task An_already_approved_member_cannot_be_approved_again()
    {
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        var response = await marion.PostJsonAsync(
            $"/moderation/members/{SeedData.Priya.Id}/approve",
            new { },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    // A moderator of one parish decides about their own parish and no other. Not found rather than
    // forbidden, because the member is simply absent from what they can see - L2-089.
    [Fact]
    public async Task A_moderator_cannot_decide_about_another_congregations_member()
    {
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        var response = await marion.PostJsonAsync(
            $"/moderation/members/{SeedData.Hank.Id}/decline",
            new { },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        (await StatusOfAsync(SeedData.Hank.Id)).ShouldBe(MemberStatus.Approved);
    }

    /// <summary>Somebody who has redeemed a code and filled the form in, waiting on a moderator.</summary>
    private async Task<Guid> AnApplicantAsync(string emailAddress, string displayName, string reason)
    {
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        var issued = await marion.PostJsonAsync("/invites", new { }, TestContext.Current.CancellationToken);

        issued.StatusCode.ShouldBe(HttpStatusCode.Created);

        var code = (await issued.ReadAsync<IssuedInviteCode>()).Code;

        using var stranger = Api.CreateClient();

        var redeemed = await stranger.PostJsonAsync(
            "/invites/redeem",
            new { code },
            TestContext.Current.CancellationToken);

        redeemed.StatusCode.ShouldBe(HttpStatusCode.OK);

        var joined = await stranger.PostJsonAsync(
            "/joining/profile",
            new
            {
                joiningToken = (await redeemed.ReadAsync<RedeemedInviteCode>()).JoiningToken,
                emailAddress,
                displayName,
                neighbourhood = SeedData.StAidans.Neighbourhoods[0],
                reasonForJoining = reason,
            },
            TestContext.Current.CancellationToken);

        joined.StatusCode.ShouldBe(HttpStatusCode.Created);

        return (await joined.ReadAsync<JoinedCongregation>()).MemberId;
    }

    private static async Task<IReadOnlyList<PendingMemberBody>> PendingAsync(HttpClient client)
    {
        var response = await client.GetAsync("/moderation/members", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        return await response.ReadAsync<IReadOnlyList<PendingMemberBody>>();
    }

    private static async Task<DecidedMember> DecideAsync(HttpClient client, Guid memberId, string decision)
    {
        var response = await client.PostJsonAsync(
            $"/moderation/members/{memberId}/{decision}",
            new { },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        return await response.ReadAsync<DecidedMember>();
    }

    private static async Task<HttpStatusCode> BoardStatusAsync(HttpClient client) =>
        (await client.GetAsync("/board", TestContext.Current.CancellationToken)).StatusCode;

    private Task<MemberStatus> StatusOfAsync(Guid memberId) =>
        Api.QueryAsync(context => context
            .Set<Member>()
            .IgnoreQueryFilters()
            .Where(member => member.Id == memberId)
            .Select(member => member.Status)
            .SingleAsync());
}
