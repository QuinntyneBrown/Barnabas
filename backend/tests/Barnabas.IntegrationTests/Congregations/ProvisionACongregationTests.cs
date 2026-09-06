using System.Net;
using Barnabas.Domain.Congregations;
using Barnabas.Domain.Members;
using Barnabas.IntegrationTests.Fixtures;
using Barnabas.Infrastructure.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;

namespace Barnabas.IntegrationTests.Congregations;

// Acceptance Test
// Traces to: L2-001, L2-002, L2-003, L2-004, L2-095
// Description: A congregation is brought into being by an administrator, with a slug that is
// unique across the deployment and cannot be changed afterwards, its own ordered neighbourhoods,
// and somebody able to run it.
//
// An administrator belongs to the platform congregation rather than to a parish, which is what
// lets provisioning work through the ordinary sign-in and the ordinary role gate. ADR-0002.
public sealed class ProvisionACongregationTests : AcceptanceTest
{
    public ProvisionACongregationTests(BarnabasApiFactory api)
        : base(api)
    {
    }

    // L2-001 AC1: Given no congregation with the slug `st-aidans`, when an administrator posts a
    // congregation with that slug, then it is created and returned with an identifier.
    [Fact]
    public async Task An_administrator_provisions_a_congregation()
    {
        using var ada = await Api.ClientForAsync(SeedData.Ada.Id);

        var response = await ada.PostJsonAsync("/congregations", StMarks(), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var provisioned = await response.ReadAsync<ProvisionedCongregation>();

        provisioned.CongregationId.ShouldNotBe(Guid.Empty);
        provisioned.Slug.ShouldBe("st-marks");
        provisioned.Name.ShouldBe("St. Mark's");

        // With a moderator, because a congregation nobody can issue an invite for is one nobody
        // can ever join. Beyond what L2-001 asks; see ADR-0002.
        var founding = await FoundingModeratorOfAsync(provisioned.CongregationId);

        founding.ShouldNotBeNull();
        founding.Role.ShouldBe(MemberRole.Moderator);
        founding.Status.ShouldBe(MemberStatus.Approved);
    }

    // L2-001 AC2: Given a congregation with the slug `st-aidans`, when an administrator posts
    // another with the same slug, then the response is 409 Conflict and no second congregation is
    // created.
    [Fact]
    public async Task A_slug_another_congregation_holds_is_refused()
    {
        using var ada = await Api.ClientForAsync(SeedData.Ada.Id);

        var before = await CountCongregationsAsync();

        var response = await ada.PostJsonAsync(
            "/congregations",
            StMarks(slug: SeedData.StAidans.Slug),
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);

        (await CountCongregationsAsync()).ShouldBe(before);
    }

    // The same rule under a different spelling. The slug is lower-cased on the way in, so a plain
    // unique index settles it without depending on the server's collation.
    [Fact]
    public async Task A_slug_differing_only_in_case_is_the_same_slug()
    {
        using var ada = await Api.ClientForAsync(SeedData.Ada.Id);

        var response = await ada.PostJsonAsync(
            "/congregations",
            StMarks(slug: "ST-AIDANS"),
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    // L2-001 AC3: Given an existing congregation, when an administrator attempts to change its
    // slug, then the response is 400 Bad Request and the slug is unchanged.
    [Fact]
    public async Task A_slug_cannot_be_changed()
    {
        using var ada = await Api.ClientForAsync(SeedData.Ada.Id);

        var response = await ada.PutRawJsonAsync(
            $"/congregations/{SeedData.StAidans.Id}",
            """
            {
              "name": "St. Aidan's",
              "neighbourhoods": ["Riverdale", "Leslieville"],
              "slug": "st-aidans-renamed"
            }
            """,
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.ReadInvalidFieldsAsync()).ShouldContain("slug");

        (await SlugOfAsync(SeedData.StAidans.Id)).ShouldBe(SeedData.StAidans.Slug);
    }

    // L2-001 AC4: Given a caller who is not an administrator, when they post a congregation, then
    // the response is 403 Forbidden.
    //
    // L2-095 AC1 as well: the role is a property of the caller, so refusing it discloses nothing
    // about any resource and answers 403 rather than 404.
    [Fact]
    public async Task A_member_who_is_not_an_administrator_may_not_provision()
    {
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        var before = await CountCongregationsAsync();

        var response = await priya.PostJsonAsync(
            "/congregations",
            StMarks(),
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        (await CountCongregationsAsync()).ShouldBe(before);
    }

    // A moderator is not an administrator. The two roles are ordered, and the gate is the higher
    // one.
    [Fact]
    public async Task A_moderator_may_not_provision_either()
    {
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);

        var response = await marion.PostJsonAsync(
            "/congregations",
            StMarks(),
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    // L2-095 AC1: an anonymous caller on a role-gated route is refused rather than faulting.
    [Fact]
    public async Task An_anonymous_caller_is_refused_rather_than_faulting()
    {
        using var stranger = Api.CreateClient();

        var response = await stranger.PostJsonAsync(
            "/congregations",
            StMarks(),
            TestContext.Current.CancellationToken);

        // 401 or 403 - either is a refusal. What matters is that reading a role nobody has does
        // not answer 500.
        response.StatusCode.ShouldBeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden);
    }

    // A malformed slug is reported by name rather than stored.
    [Theory]
    [InlineData("St Aidans")]
    [InlineData("st_aidans")]
    [InlineData("-st-aidans")]
    [InlineData("ab")]
    public async Task A_slug_that_is_not_url_safe_is_refused_naming_the_field(string slug)
    {
        using var ada = await Api.ClientForAsync(SeedData.Ada.Id);

        var response = await ada.PostJsonAsync(
            "/congregations",
            StMarks(slug: slug),
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.ReadInvalidFieldsAsync()).ShouldContain("Slug");
    }

    // L2-002 AC1: Given a congregation, when an administrator sets its neighbourhoods to a list of
    // names, then those names are returned in the order supplied.
    [Fact]
    public async Task Neighbourhoods_come_back_in_the_order_they_were_given()
    {
        using var ada = await Api.ClientForAsync(SeedData.Ada.Id);

        string[] inThisOrder = ["The Beaches", "Riverdale", "Leslieville"];

        var response = await ada.PutJsonAsync(
            $"/congregations/{SeedData.StAidans.Id}",
            new { name = SeedData.StAidans.Name, neighbourhoods = inThisOrder },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var configured = await response.ReadAsync<ConfiguredCongregation>();

        // The order is part of the value: it is the order a member is offered them in, so a list
        // that came back sorted would be a different answer from the one that was given.
        configured.Neighbourhoods.ShouldBe(inThisOrder);
    }

    [Fact]
    public async Task A_neighbourhood_named_twice_is_refused()
    {
        using var ada = await Api.ClientForAsync(SeedData.Ada.Id);

        var response = await ada.PutJsonAsync(
            $"/congregations/{SeedData.StAidans.Id}",
            new { name = SeedData.StAidans.Name, neighbourhoods = new[] { "Riverdale", "riverdale" } },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.ReadInvalidFieldsAsync()).ShouldContain("Neighbourhoods");
    }

    // L2-004 AC1: Given a member of a congregation named `St. Aidan's`, when they open the board,
    // then the heading names that congregation.
    //
    // The API half: the name is served rather than assumed, so no screen hard-codes a parish.
    [Fact]
    public async Task A_member_reads_their_own_congregation_and_no_other()
    {
        using var marion = await Api.ClientForAsync(SeedData.Marion.Id);
        using var hank = await Api.ClientForAsync(SeedData.Hank.Id);

        var hers = await (await marion.GetAsync("/congregation", TestContext.Current.CancellationToken))
            .ReadAsync<CongregationBody>();

        var his = await (await hank.GetAsync("/congregation", TestContext.Current.CancellationToken))
            .ReadAsync<CongregationBody>();

        hers.Name.ShouldBe(SeedData.StAidans.Name);
        hers.Slug.ShouldBe(SeedData.StAidans.Slug);
        hers.Neighbourhoods.ShouldBe(SeedData.StAidans.Neighbourhoods);

        // L2-004 AC2: never another congregation's.
        his.Name.ShouldBe(SeedData.StBrigids.Name);
        his.Name.ShouldNotBe(SeedData.StAidans.Name);
    }

    // L2-003 AC1: Given an approved member, when an administrator grants them the moderator role
    // in their congregation, then subsequent moderation requests from that member succeed.
    [Fact]
    public async Task An_administrator_designates_a_moderator()
    {
        using var ada = await Api.ClientForAsync(SeedData.Ada.Id);

        var response = await ada.PostJsonAsync(
            $"/congregations/{SeedData.StAidans.Id}/moderators/{SeedData.Priya.Id}",
            new { },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        (await RoleOfAsync(SeedData.Priya.Id)).ShouldBe(MemberRole.Moderator);
    }

    // L2-092: a moderator's authority stops at the congregation that granted it, and so does an
    // administrator's reach - naming the wrong congregation finds no member at all.
    [Fact]
    public async Task Designating_a_member_of_another_congregation_finds_nobody()
    {
        using var ada = await Api.ClientForAsync(SeedData.Ada.Id);

        var response = await ada.PostJsonAsync(
            $"/congregations/{SeedData.StAidans.Id}/moderators/{SeedData.Hank.Id}",
            new { },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        (await RoleOfAsync(SeedData.Hank.Id)).ShouldBe(MemberRole.Member);
    }

    // L2-003 AC2 is the moderation queue's own 403, which the moderation slice states. This is the
    // other half of the same rule: an ordinary member cannot hand out the role.
    [Fact]
    public async Task A_member_may_not_designate_a_moderator()
    {
        using var priya = await Api.ClientForAsync(SeedData.Priya.Id);

        var response = await priya.PostJsonAsync(
            $"/congregations/{SeedData.StAidans.Id}/moderators/{SeedData.Grace.Id}",
            new { },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await RoleOfAsync(SeedData.Grace.Id)).ShouldBe(MemberRole.Member);
    }

    private static object StMarks(string? slug = null) => new
    {
        name = "St. Mark's",
        slug = slug ?? "st-marks",
        neighbourhoods = new[] { "Parkdale", "Roncesvalles" },
        foundingModeratorEmail = "warden@example.com",
        foundingModeratorDisplayName = "The Warden",
    };

    private Task<int> CountCongregationsAsync() =>
        Api.QueryAsync(context => context.Set<Congregation>().IgnoreQueryFilters().CountAsync());

    private Task<string> SlugOfAsync(Guid congregationId) =>
        Api.QueryAsync(context => context
            .Set<Congregation>()
            .IgnoreQueryFilters()
            .Where(congregation => congregation.Id == congregationId)
            .Select(congregation => congregation.Slug)
            .SingleAsync());

    private Task<MemberRole> RoleOfAsync(Guid memberId) =>
        Api.QueryAsync(context => context
            .Set<Member>()
            .IgnoreQueryFilters()
            .Where(member => member.Id == memberId)
            .Select(member => member.Role)
            .SingleAsync());

    private Task<Member?> FoundingModeratorOfAsync(Guid congregationId) =>
        Api.QueryAsync(context => context
            .Set<Member>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(member => member.CongregationId == congregationId));
}
