using MediatR;

namespace Barnabas.Application.Joining.SubmitJoiningProfile;

/// <summary>
/// Finishes joining: who they are, and where in the parish.
/// </summary>
/// <remarks>
/// Anonymous, and authorised by the joining token rather than by a session. The token names the
/// congregation, so there is no identifier here for a caller to change.
/// <para>
/// The email address is not in <c>L2-010</c>, and is collected because <c>Member</c> requires one
/// and sign-in looks a member up by it — a member created without one could never sign in. The
/// reason for joining is not in <c>L2-010</c> either, and is collected because <c>L2-085</c>
/// requires a moderator to read it. Both are recorded in ADR-0002 rather than absorbed quietly.
/// </remarks>
public sealed record SubmitJoiningProfileCommand(
    string JoiningToken,
    string EmailAddress,
    string DisplayName,
    string Neighbourhood,
    string? ReasonForJoining) : IRequest<JoinedCongregationResult>;
