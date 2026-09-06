using System.Net;
using Barnabas.Domain.Members;
using Barnabas.IntegrationTests.Fixtures;
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

        members.ShouldBe(4);
    }
}
