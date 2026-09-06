using Barnabas.Domain.Members;

namespace Barnabas.Application.Common.Persistence;

/// <summary>
/// The one way to write into a congregation that is not the caller's own.
/// </summary>
/// <remarks>
/// A deliberate hole in the tenant boundary, and a narrow one — the second after
/// <see cref="IAuthenticationStore"/>. It exists because provisioning creates the first member of
/// a congregation that has just come into being, and every ordinary write is stamped with the
/// caller's congregation: an administrator creating St. Mark's founding moderator would otherwise
/// put them in the platform congregation.
/// <para>
/// It can create a founding moderator and read a member by an explicit
/// <c>(congregationId, memberId)</c> pair, and it can do nothing else. It never returns a listing,
/// a request, or a thread, and nothing here is reachable without <c>MemberRole.Administrator</c>.
/// </para>
/// </remarks>
public interface IProvisioningStore
{
    /// <summary>Whether any congregation already holds this slug.</summary>
    Task<bool> SlugIsTakenAsync(string slug, CancellationToken cancellationToken);

    /// <summary>Creates the first member of a congregation, as its moderator.</summary>
    Task AddFoundingModeratorAsync(
        Guid memberId,
        Guid congregationId,
        string emailAddress,
        string displayName,
        string neighbourhood,
        CancellationToken cancellationToken);

    /// <summary>Whether the address is already registered anywhere in the deployment.</summary>
    Task<bool> EmailIsRegisteredAsync(string emailAddress, CancellationToken cancellationToken);

    /// <summary>
    /// One member of one congregation, by both identifiers.
    /// </summary>
    /// <remarks>
    /// Both are required. A member reachable by identifier alone would let an administrator grant
    /// a role in a congregation they did not name, and a wrong pair is simply not found.
    /// </remarks>
    Task<Member?> FindMemberAsync(Guid congregationId, Guid memberId, CancellationToken cancellationToken);

    Task SaveAsync(CancellationToken cancellationToken);
}
