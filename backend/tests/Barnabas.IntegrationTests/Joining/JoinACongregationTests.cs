using System.Net;
using Barnabas.Domain.Congregations;
using Barnabas.Domain.Members;
using Barnabas.IntegrationTests.Fixtures;
using Barnabas.Infrastructure.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;

namespace Barnabas.IntegrationTests.Joining;

// Acceptance Test
// Traces to: L2-005, L2-006, L2-007, L2-008, L2-009, L2-010, L2-011, L2-090
// Description: A moderator issues a code; somebody redeems it, supplies a profile, and waits for
// a moderator to let them in. A code is spent exactly once, and a code that cannot be used says
// only that.
public sealed class JoinACongregationTests : AcceptanceTest
{
    public JoinACongregationTests(BarnabasApiFactory api)
        : base(api)
    {
    }

    // L2-005 AC1: Given a moderator, when they issue an invite code, then a code of at least six
    // characters and an expiry date are returned.
    [Fact]
    public async Task A_moderator_issues_a_code()
    {
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        var issued = await IssueAsync(marion);

        issued.Code.Length.ShouldBeGreaterThanOrEqualTo(6);
        issued.ExpiresAt.ShouldBeGreaterThan(Api.Clock.GetUtcNow());

        // Drawn from an alphabet nobody misreads. A code is spoken aloud and typed by hand, so
        // I, O, 0 and 1 are absent by construction rather than by luck.
        issued.Code.ShouldAllBe(character => InviteCode.Alphabet.Contains(character));
    }

    // L2-005 AC2: Given two codes issued in the same congregation, when they are compared, then
    // they differ.
    [Fact]
    public async Task Two_codes_differ()
    {
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        var first = await IssueAsync(marion);
        var second = await IssueAsync(marion);

        second.Code.ShouldNotBe(first.Code);
    }

    // L2-005 AC3: Given a member who is not a moderator, when they issue an invite code, then the
    // response is 403 Forbidden.
    [Fact]
    public async Task An_ordinary_member_may_not_issue_a_code()
    {
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        var response = await priya.PostJsonAsync("/invites", new { }, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    // L2-006 AC1: Given an unexpired, unredeemed code, when it is redeemed, then the joining flow
    // begins for the issuing congregation.
    [Fact]
    public async Task A_valid_code_begins_joining_and_names_the_congregation()
    {
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        var issued = await IssueAsync(marion);

        using var stranger = Api.CreateClient();

        var redeemed = await RedeemAsync(stranger, issued.Code);

        redeemed.CongregationName.ShouldBe(SeedData.StAidans.Name);
        redeemed.Neighbourhoods.ShouldBe(SeedData.StAidans.Neighbourhoods);

        // A bearer token rather than the congregation's identifier: handing back an identifier
        // would let the next call name any congregation it liked.
        redeemed.JoiningToken.ShouldNotBeNullOrWhiteSpace();
    }

    // A code is read the way somebody typed it - lower case, and with the space they put in the
    // middle reading it off a card.
    [Fact]
    public async Task A_code_is_read_the_way_it_was_typed()
    {
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        var issued = await IssueAsync(marion);

        using var stranger = Api.CreateClient();

        var asTyped = issued.Code[..4].ToLowerInvariant() + " " + issued.Code[4..].ToLowerInvariant();

        var redeemed = await RedeemAsync(stranger, asTyped);

        redeemed.CongregationName.ShouldBe(SeedData.StAidans.Name);
    }

    // L2-007 AC1: Given an unknown code, when it is redeemed, then the response is 404 Not Found.
    [Fact]
    public async Task An_unrecognised_code_is_not_found()
    {
        using var stranger = Api.CreateClient();

        var response = await stranger.PostJsonAsync(
            "/invites/redeem",
            new { code = "ZZZZ9999" },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // L2-008: Given a code past its expiry, when it is redeemed, then it is not redeemable (410)
    // and the screen says who to ask for another.
    [Fact]
    public async Task An_expired_code_is_gone()
    {
        var expired = await AnExpiredCodeAsync();

        using var stranger = Api.CreateClient();

        var response = await stranger.PostJsonAsync(
            "/invites/redeem",
            new { code = expired },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Gone);
    }

    // L2-009 AC1: Given a code that has been redeemed, when it is redeemed again, then it is
    // refused.
    [Fact]
    public async Task A_code_redeems_once()
    {
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        var issued = await IssueAsync(marion);

        using var first = Api.CreateClient();
        using var second = Api.CreateClient();

        await RedeemAsync(first, issued.Code);

        var again = await second.PostJsonAsync(
            "/invites/redeem",
            new { code = issued.Code },
            TestContext.Current.CancellationToken);

        again.StatusCode.ShouldBe(HttpStatusCode.Gone);
    }

    // L2-009 AC2: Given concurrent redemption of one code, when both are attempted, then exactly
    // one succeeds.
    //
    // The rule a policy cannot make true on its own. Both callers find the code unredeemed before
    // either commits; only the conditional update can settle which of them joins.
    [Fact]
    public async Task Only_one_of_two_simultaneous_redemptions_succeeds()
    {
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        var issued = await IssueAsync(marion);

        using var first = Api.CreateClient();
        using var second = Api.CreateClient();

        var attempts = await Task.WhenAll(
            first.PostJsonAsync("/invites/redeem", new { code = issued.Code }, TestContext.Current.CancellationToken),
            second.PostJsonAsync("/invites/redeem", new { code = issued.Code }, TestContext.Current.CancellationToken));

        attempts.Count(attempt => attempt.StatusCode == HttpStatusCode.OK).ShouldBe(1);
        attempts.Count(attempt => attempt.StatusCode == HttpStatusCode.Gone).ShouldBe(1);
    }

    // L2-007 AC3: an unusable code discloses no more than that it is unusable. Expired, spent and
    // revoked read alike, because which of the three it was would say something about a code the
    // caller is not entitled to know about.
    [Fact]
    public async Task A_revoked_code_and_a_spent_code_read_alike()
    {
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        var toRevoke = await IssueAsync(marion);
        var toSpend = await IssueAsync(marion);

        var revoked = await marion.DeleteAsync(
            $"/invites/{toRevoke.InviteCodeId}",
            TestContext.Current.CancellationToken);

        revoked.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        using var stranger = Api.CreateClient();

        await RedeemAsync(stranger, toSpend.Code);

        var spentAgain = await stranger.PostJsonAsync(
            "/invites/redeem",
            new { code = toSpend.Code },
            TestContext.Current.CancellationToken);

        var revokedAttempt = await stranger.PostJsonAsync(
            "/invites/redeem",
            new { code = toRevoke.Code },
            TestContext.Current.CancellationToken);

        revokedAttempt.StatusCode.ShouldBe(spentAgain.StatusCode);

        // The same title, so nothing in the wording says which of the two conditions it was. The
        // trace identifier differs per request and discloses nothing about the code.
        (await TitleOfAsync(revokedAttempt)).ShouldBe(await TitleOfAsync(spentAgain));
    }

    // L2-090 AC2: a moderator of another congregation cannot administer this one's codes.
    [Fact]
    public async Task A_moderator_of_another_congregation_cannot_revoke_this_ones_code()
    {
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        var issued = await IssueAsync(marion);

        using var hank = await Api.ClientForAsync(SeedData.Hank.Id);

        var response = await hank.DeleteAsync(
            $"/invites/{issued.InviteCodeId}",
            TestContext.Current.CancellationToken);

        // Not found rather than forbidden. Another congregation's code is simply absent from what
        // Hank can see, and saying "forbidden" would confirm it exists.
        response.StatusCode.ShouldBeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
    }

    // L2-010 AC1: Given the joining flow, when a display name and neighbourhood are supplied, then
    // a member is created awaiting approval.
    [Fact]
    public async Task A_profile_creates_a_member_awaiting_approval()
    {
        var joining = await AJoiningTokenAsync();

        using var stranger = Api.CreateClient();

        var response = await stranger.PostJsonAsync(
            "/joining/profile",
            AProfile(joining),
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var joined = await response.ReadAsync<JoinedCongregation>();

        joined.CongregationName.ShouldBe(SeedData.StAidans.Name);
        joined.Status.ShouldBe(nameof(MemberStatus.AwaitingApproval));

        (await StatusOfAsync("newcomer@example.com")).ShouldBe(MemberStatus.AwaitingApproval);
    }

    // L2-010 AC2: Given a profile missing a required field, when it is submitted, then the
    // response is 400 Bad Request naming the field, and no member is created.
    [Fact]
    public async Task A_profile_without_a_display_name_is_refused_naming_the_field()
    {
        var joining = await AJoiningTokenAsync();

        using var stranger = Api.CreateClient();

        var response = await stranger.PostJsonAsync(
            "/joining/profile",
            AProfile(joining, displayName: string.Empty),
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.ReadInvalidFieldsAsync()).ShouldContain("DisplayName");

        (await CountMembersAsync()).ShouldBe(5);
    }

    // L2-002 AC2: Given a congregation whose neighbourhoods do not include `Rosedale`, when a
    // member sets their neighbourhood to `Rosedale`, then the response is 400 naming the field.
    [Fact]
    public async Task A_neighbourhood_the_congregation_does_not_offer_is_refused_naming_the_field()
    {
        var joining = await AJoiningTokenAsync();

        using var stranger = Api.CreateClient();

        var response = await stranger.PostJsonAsync(
            "/joining/profile",
            AProfile(joining, neighbourhood: "Rosedale"),
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.ReadInvalidFieldsAsync()).ShouldContain("neighbourhood");
    }

    // An invalid field does not cost somebody their code. Burning the capability on a typo would
    // send them back to a moderator for a fresh one.
    [Fact]
    public async Task A_refused_profile_leaves_the_joining_session_open()
    {
        var joining = await AJoiningTokenAsync();

        using var stranger = Api.CreateClient();

        var refused = await stranger.PostJsonAsync(
            "/joining/profile",
            AProfile(joining, displayName: string.Empty),
            TestContext.Current.CancellationToken);

        refused.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var corrected = await stranger.PostJsonAsync(
            "/joining/profile",
            AProfile(joining),
            TestContext.Current.CancellationToken);

        corrected.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    // The capability is single use, decided the same way the code is.
    [Fact]
    public async Task A_joining_session_is_spent_once()
    {
        var joining = await AJoiningTokenAsync();

        using var stranger = Api.CreateClient();

        (await stranger.PostJsonAsync("/joining/profile", AProfile(joining), TestContext.Current.CancellationToken))
            .StatusCode.ShouldBe(HttpStatusCode.Created);

        var again = await stranger.PostJsonAsync(
            "/joining/profile",
            AProfile(joining, email: "someone-else@example.com"),
            TestContext.Current.CancellationToken);

        again.StatusCode.ShouldBe(HttpStatusCode.Gone);
    }

    // L2-011 AC1: Given a member awaiting approval, when they request the board, then the response
    // is 403 Forbidden.
    [Fact]
    public async Task A_member_awaiting_approval_may_not_reach_the_board()
    {
        var memberId = await AJoinedMemberAsync();

        using var newcomer = await Api.ClientForAsync(memberId);

        var board = await newcomer.GetAsync("/board", TestContext.Current.CancellationToken);

        // Forbidden, not unauthorised. They are who they say they are; they are simply not on the
        // board yet, and 401 would send them round a sign-in loop they cannot leave.
        board.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    // L2-011 AC2: Given a member awaiting approval, when they sign in, then they are told their
    // request is with the moderators, and which congregation it is.
    [Fact]
    public async Task A_member_awaiting_approval_can_still_see_which_congregation_they_are_waiting_on()
    {
        var memberId = await AJoinedMemberAsync();

        using var newcomer = await Api.ClientForAsync(memberId);

        var response = await newcomer.GetAsync("/congregation", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        (await response.ReadAsync<CongregationBody>()).Name.ShouldBe(SeedData.StAidans.Name);
    }

    // L2-011 AC3: a pending member can sign in at all. The sign-in lookup used to require an
    // approved member, which made the awaiting screen unreachable.
    [Fact]
    public async Task A_member_awaiting_approval_can_sign_in()
    {
        await AJoinedMemberAsync();

        using var stranger = Api.CreateClient();

        var response = await stranger.PostJsonAsync(
            "/sessions/link",
            new { emailAddress = "newcomer@example.com" },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);

        Api.Outbox.LatestFor("newcomer@example.com").ShouldNotBeNull();
    }

    private async Task<IssuedInviteCode> IssueAsync(HttpClient client)
    {
        var response = await client.PostJsonAsync("/invites", new { }, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        return await response.ReadAsync<IssuedInviteCode>();
    }

    private static async Task<RedeemedInviteCode> RedeemAsync(HttpClient client, string code)
    {
        var response = await client.PostJsonAsync(
            "/invites/redeem",
            new { code },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        return await response.ReadAsync<RedeemedInviteCode>();
    }

    private async Task<string> AJoiningTokenAsync()
    {
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        var issued = await IssueAsync(marion);

        using var stranger = Api.CreateClient();

        return (await RedeemAsync(stranger, issued.Code)).JoiningToken;
    }

    private async Task<Guid> AJoinedMemberAsync()
    {
        var joining = await AJoiningTokenAsync();

        using var stranger = Api.CreateClient();

        var response = await stranger.PostJsonAsync(
            "/joining/profile",
            AProfile(joining),
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        return (await response.ReadAsync<JoinedCongregation>()).MemberId;
    }

    /// <summary>A code the clock has already passed, arranged rather than waited for.</summary>
    private async Task<string> AnExpiredCodeAsync()
    {
        var code = "EXPIRED9";

        await Api.ArrangeAsync(async context =>
        {
            context.Add(new InviteCode(
                Guid.NewGuid(),
                SeedData.StAidans.Id,
                code,
                Api.Clock.GetUtcNow().AddDays(-1)));

            await context.SaveChangesAsync();
        });

        return code;
    }

    private static object AProfile(
        string joiningToken,
        string? displayName = null,
        string? neighbourhood = null,
        string? email = null) => new
    {
        joiningToken,
        emailAddress = email ?? "newcomer@example.com",
        displayName = displayName ?? "Newcomer N.",
        neighbourhood = neighbourhood ?? SeedData.StAidans.Neighbourhoods[0],
        reasonForJoining = "New to the parish and keen to lend a hand.",
    };

    /// <summary>The problem's title, which is the part that could say too much.</summary>
    private static async Task<string?> TitleOfAsync(HttpResponseMessage response)
    {
        using var document = System.Text.Json.JsonDocument.Parse(await response.ReadBodyAsync());

        return document.RootElement.TryGetProperty("title", out var title) ? title.GetString() : null;
    }

    private Task<int> CountMembersAsync() =>
        Api.QueryAsync(context => context.Set<Member>().IgnoreQueryFilters().CountAsync());

    private Task<MemberStatus> StatusOfAsync(string emailAddress) =>
        Api.QueryAsync(context => context
            .Set<Member>()
            .IgnoreQueryFilters()
            .Where(member => member.EmailAddress == emailAddress)
            .Select(member => member.Status)
            .SingleAsync());
}
