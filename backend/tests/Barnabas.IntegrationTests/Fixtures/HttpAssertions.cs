using System.Net;

namespace Barnabas.IntegrationTests.Fixtures;

/// <summary>Status assertions that say what the server was complaining about.</summary>
public static class HttpAssertions
{
    public static void ShouldBe(
        this HttpStatusCode actual,
        HttpStatusCode expected,
        BarnabasApiFactory api)
    {
        ArgumentNullException.ThrowIfNull(api);

        if (actual == expected)
        {
            return;
        }

        throw new ShouldAssertException(
            $"Expected {expected} but the API answered {actual}.{Environment.NewLine}{api.ServerErrors.Summary}");
    }
}
