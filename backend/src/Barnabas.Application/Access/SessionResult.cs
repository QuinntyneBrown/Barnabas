using Barnabas.Domain.Members;

namespace Barnabas.Application.Access;

/// <summary>
/// Everything a successful sign-in or renewal produces.
/// </summary>
/// <remarks>
/// The refresh token is here because the controller has to put it somewhere, and nowhere else
/// in the application layer knows it exists. It does not reach the response body: the API
/// writes it into an HttpOnly cookie, which is the only place a long-lived secret belongs once
/// a browser is holding it.
/// </remarks>
/// <remarks>
/// <see cref="Status"/> travels with it so a client knows whether the board is open to this
/// member without asking a second question. It is renewed on every refresh, and the guard
/// refreshes on every navigation, so a moderator's approval takes effect on the member's next
/// visit rather than their next sign-in - which is what <c>L2-086</c> asks for.
/// </remarks>
public sealed record SessionResult(
    Guid SessionId,
    string AccessToken,
    DateTimeOffset ExpiresOn,
    string RefreshToken,
    MemberStatus Status,
    MemberRole Role);
