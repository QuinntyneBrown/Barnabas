using System.Net;

namespace Barnabas.IntegrationTests.Fixtures;

/// <summary>Reading and appending to a thread, so the tests read as the conversation does.</summary>
public static class Conversation
{
    public static async Task SayAsync(this HttpClient client, Guid threadId, string body)
    {
        ArgumentNullException.ThrowIfNull(client);

        var response = await client.PostJsonAsync($"/threads/{threadId}/messages", new { body });

        if (response.StatusCode != HttpStatusCode.Created)
        {
            throw new InvalidOperationException($"Sending a message answered {response.StatusCode}.");
        }
    }

    public static async Task<ThreadDetailBody> OpenAsync(this HttpClient client, Guid threadId)
    {
        ArgumentNullException.ThrowIfNull(client);

        return await (await client.GetAsync($"/threads/{threadId}")).ReadAsync<ThreadDetailBody>();
    }

    public static async Task<IReadOnlyList<ThreadSummaryBody>> ThreadsAsync(this HttpClient client)
    {
        ArgumentNullException.ThrowIfNull(client);

        return await (await client.GetAsync("/threads")).ReadAsync<IReadOnlyList<ThreadSummaryBody>>();
    }
}
