using System.Net;
using System.Net.Http.Headers;
using Barnabas.Api.Security;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Barnabas.IntegrationTests.Fixtures;

/// <summary>
/// One member signed in on one device, holding what that device would hold.
/// </summary>
/// <remarks>
/// Cookies are handled by hand rather than by the client, because most of what the session
/// criteria ask is which token was presented. A client that quietly replaced a retired refresh
/// token with a fresh one would make "present the same refresh token again" untestable, and
/// several of the L2-018 and L2-019 criteria are exactly that.
/// </remarks>
public sealed class Device : IDisposable
{
    private readonly BarnabasApiFactory _api;
    private readonly HttpClient _client;

    private Device(BarnabasApiFactory api)
    {
        _api = api;
        _client = api.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
    }

    public string AccessToken { get; private set; } = string.Empty;

    public string RefreshToken { get; private set; } = string.Empty;

    public Guid SessionId { get; private set; }

    /// <summary>Signs in the way a member does: ask for a link, follow it, exchange it.</summary>
    public static async Task<Device> SignInAsync(BarnabasApiFactory api, string emailAddress)
    {
        ArgumentNullException.ThrowIfNull(api);

        var device = new Device(api);

        var requested = await device._client.PostJsonAsync("/sessions/link", new { emailAddress });

        if (requested.StatusCode != HttpStatusCode.Accepted)
        {
            throw new InvalidOperationException($"Requesting a sign-in link answered {requested.StatusCode}.");
        }

        var token = api.Outbox.LatestFor(emailAddress)?.Token
            ?? throw new InvalidOperationException($"No sign-in link was dispatched to {emailAddress}.");

        var exchanged = await device._client.PostJsonAsync("/sessions", new { token });

        if (exchanged.StatusCode != HttpStatusCode.OK)
        {
            throw new InvalidOperationException($"Exchanging the sign-in link answered {exchanged.StatusCode}.");
        }

        await device.AdoptAsync(exchanged);

        return device;
    }

    public async Task<HttpStatusCode> ReadBoardAsync() =>
        (await SendAsync(HttpMethod.Get, "/board")).StatusCode;

    public async Task<HttpResponseMessage> GetAsync(string route) => await SendAsync(HttpMethod.Get, route);

    public async Task<HttpResponseMessage> PostAsync(string route, object payload) =>
        await SendAsync(HttpMethod.Post, route, payload);

    /// <summary>Renews, and keeps what the renewal returned.</summary>
    public async Task<HttpStatusCode> RenewAsync()
    {
        var response = await RenewRequestAsync(RefreshToken);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            await AdoptAsync(response);
        }

        return response.StatusCode;
    }

    /// <summary>
    /// Renews with a token the caller names, and keeps nothing.
    /// </summary>
    /// <remarks>
    /// Deliberately non-adopting: this is how a retired token, or the same token twice at once,
    /// is presented.
    /// </remarks>
    public async Task<HttpStatusCode> RenewWithAsync(string refreshToken) =>
        (await RenewRequestAsync(refreshToken)).StatusCode;

    public async Task<HttpStatusCode> SignOutAsync() =>
        (await SendAsync(HttpMethod.Delete, "/sessions")).StatusCode;

    public void Dispose() => _client.Dispose();

    private async Task<HttpResponseMessage> RenewRequestAsync(string refreshToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/sessions/refresh");

        request.Headers.Add("Cookie", $"{RefreshCookieOptions.CookieName}={refreshToken}");

        return await _client.SendAsync(request);
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string route, object? payload = null)
    {
        using var request = new HttpRequestMessage(method, route)
        {
            Content = payload is null ? null : Json.From.Of(payload),
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);

        return await _client.SendAsync(request);
    }

    private async Task AdoptAsync(HttpResponseMessage response)
    {
        var session = await response.ReadAsync<SessionBody>();

        AccessToken = session.AccessToken;
        SessionId = session.SessionId;
        RefreshToken = ReadRefreshCookie(response) ?? RefreshToken;
    }

    private static string? ReadRefreshCookie(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            return null;
        }

        var prefix = $"{RefreshCookieOptions.CookieName}=";

        return cookies
            .Select(cookie => cookie.Split(';')[0])
            .Where(pair => pair.StartsWith(prefix, StringComparison.Ordinal))
            .Select(pair => pair[prefix.Length..])
            .FirstOrDefault(value => value.Length > 0);
    }
}
