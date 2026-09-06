using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Barnabas.IntegrationTests;

// Acceptance Test
// Traces to: none — Stage 0 scaffolding
// Description: The API host starts and serves a request.
public sealed class ApiStartsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ApiStartsTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Health_endpoint_answers()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/health", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
