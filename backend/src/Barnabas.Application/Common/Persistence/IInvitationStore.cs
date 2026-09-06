using Barnabas.Domain.Congregations;
using Barnabas.Domain.Members;

namespace Barnabas.Application.Common.Persistence;

/// <summary>
/// The one sanctioned read and write before a member exists.
/// </summary>
/// <remarks>
/// The third deliberate hole in the tenant boundary, after <see cref="IAuthenticationStore"/> and
/// <see cref="IProvisioningStore"/>, and kept as narrow as both. Redeeming a code is anonymous:
/// nobody is signed in, so no congregation is in scope, and the code is the thing that reveals
/// which congregation is being joined. A filtered read cannot serve that lookup, and
/// <c>InviteCodes</c> is gated — going through the ordinary set would raise
/// <c>CongregationContextMissingException</c> and answer 500 where <c>L2-007</c> asks for 404.
/// <para>
/// Nothing here returns a listing, a request, or a thread. It sees invite codes, joining sessions,
/// congregations and the member it creates, and adding anything else would widen the hole.
/// </para>
/// </remarks>
public interface IInvitationStore
{
    /// <summary>
    /// Spends the code, if it is there to spend.
    /// </summary>
    /// <remarks>
    /// One conditional update whose affected-row count decides the winner, exactly as the sign-in
    /// token's does. Two people typing the same code at the same moment both find it redeemable
    /// before either commits; only the statement can settle which of them joins.
    /// <para>
    /// Expiry and revocation are deliberately absent from the update's conditions. They are not
    /// races — a clock reading is not something two callers disagree about — so the returned
    /// entity answers them, and spending a code that was already dead costs nothing.
    /// </para>
    /// </remarks>
    Task<InviteCode?> TryRedeemAsync(string normalisedCode, DateTimeOffset asOf, CancellationToken cancellationToken);

    /// <summary>The code as it stands, so an unknown one can be told from an unusable one.</summary>
    Task<InviteCode?> FindCodeAsync(string normalisedCode, CancellationToken cancellationToken);

    Task AddJoiningSessionAsync(JoiningSession session, CancellationToken cancellationToken);

    /// <summary>Spends the entitlement to finish joining. Single use, decided the same way.</summary>
    Task<JoiningSession?> TryConsumeJoiningSessionAsync(
        string tokenHash,
        DateTimeOffset asOf,
        CancellationToken cancellationToken);

    Task<Congregation?> FindCongregationAsync(Guid congregationId, CancellationToken cancellationToken);

    /// <summary>Whether the address already belongs to somebody, anywhere in the deployment.</summary>
    Task<bool> EmailIsRegisteredAsync(string emailAddress, CancellationToken cancellationToken);

    /// <summary>
    /// Creates the member the joining flow has been building up to.
    /// </summary>
    /// <remarks>
    /// Written directly rather than through the tracked set for the same reason the founding
    /// moderator is: no congregation is in scope, so <c>Members</c> would refuse the write
    /// outright.
    /// </remarks>
    Task AddMemberAsync(Member member, CancellationToken cancellationToken);
}
