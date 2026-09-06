namespace Barnabas.Application.Joining.SubmitJoiningProfile;

/// <summary>
/// What somebody is told once they have joined and are waiting.
/// </summary>
/// <remarks>
/// No session comes back with it. They are not on the board yet, and handing over one that could
/// not reach it would be worse than saying plainly that a moderator has to let them in, which is
/// what <c>L2-011</c> asks for.
/// </remarks>
public sealed record JoinedCongregationResult(Guid MemberId, string CongregationName, string Status);
