using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Barnabas.IntegrationTests.Fixtures;

/// <summary>Reading and writing the API's payloads without ceremony in each test.</summary>
public static class Json
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static Task<HttpResponseMessage> PostJsonAsync(
        this HttpClient client,
        string route,
        object payload,
        CancellationToken cancellationToken = default) =>
        client.PostAsJsonAsync(route, payload, Options, cancellationToken);

    /// <summary>
    /// Posts a body the command has no property for.
    /// </summary>
    /// <remarks>
    /// Needed because the interesting cases are exactly the ones a typed client cannot express:
    /// a price on a Lend listing, and a field the command has never heard of.
    /// </remarks>
    public static Task<HttpResponseMessage> PostRawJsonAsync(
        this HttpClient client,
        string route,
        string json,
        CancellationToken cancellationToken = default) =>
        client.PostAsync(route, new StringContent(json, Encoding.UTF8, "application/json"), cancellationToken);

    /// <summary>Builds a JSON body for a request the tests assemble by hand.</summary>
    public static class From
    {
        public static HttpContent Of(object payload) => JsonContent.Create(payload, options: Options);
    }

    public static async Task<T> ReadAsync<T>(this HttpResponseMessage response)
    {
        ArgumentNullException.ThrowIfNull(response);

        var value = await response.Content.ReadFromJsonAsync<T>(Options);

        return value ?? throw new InvalidOperationException($"The response carried no {typeof(T).Name}.");
    }

    /// <summary>The field names a validation failure named, so a test can assert on them.</summary>
    public static async Task<IReadOnlyCollection<string>> ReadInvalidFieldsAsync(this HttpResponseMessage response)
    {
        ArgumentNullException.ThrowIfNull(response);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        if (!document.RootElement.TryGetProperty("errors", out var errors))
        {
            return [];
        }

        return [.. errors.EnumerateObject().Select(field => field.Name)];
    }

    public static async Task<string> ReadBodyAsync(this HttpResponseMessage response)
    {
        ArgumentNullException.ThrowIfNull(response);

        return await response.Content.ReadAsStringAsync();
    }
}
