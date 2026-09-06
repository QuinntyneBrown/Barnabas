using System.Net;
using Barnabas.Domain.Members;
using Barnabas.IntegrationTests.Fixtures;
using Barnabas.Infrastructure.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;

namespace Barnabas.IntegrationTests;

// Acceptance Test
// Traces to: none - harness smoke test
// Description: The API host starts, applies its schema, seeds the congregation, and serves a request.
public sealed class ApiStartsTests : AcceptanceTest
{
    public ApiStartsTests(BarnabasApiFactory api)
        : base(api)
    {
    }

    [Fact]
    public async Task Health_endpoint_answers()
    {
        using var client = Api.CreateClient();

        var response = await client.GetAsync("/health", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Seeded_congregation_is_present()
    {
        var members = await Api.QueryAsync(context =>
            context.Set<Member>().IgnoreQueryFilters().CountAsync());

        members.ShouldBe(5);
    }

    /// <summary>
    /// The seed carries somebody who can provision, and somebody who can moderate.
    /// </summary>
    /// <remarks>
    /// Everything a congregation needs to exist comes from an administrator, and everything a
    /// member needs to join comes from a moderator. A seed with neither would leave both flows
    /// untestable while looking perfectly healthy. ADR-0002.
    /// </remarks>
    [Fact]
    public async Task Seeded_congregation_has_an_administrator_and_a_moderator()
    {
        var roles = await Api.QueryAsync(context => context
            .Set<Member>()
            .IgnoreQueryFilters()
            .Where(member => member.Role != MemberRole.Member)
            .Select(member => new { member.EmailAddress, member.Role, member.CongregationId })
            .ToListAsync());

        var administrator = roles
            .Where(member => member.Role == MemberRole.Administrator)
            .ShouldHaveSingleItem();

        // In the platform congregation, not in a parish. An administrator provisions parishes and
        // is a member of none of them.
        administrator.CongregationId.ShouldBe(SeedData.Platform.Id);

        roles.ShouldContain(member =>
            member.Role == MemberRole.Moderator && member.CongregationId == SeedData.StAidans.Id);
    }
}
