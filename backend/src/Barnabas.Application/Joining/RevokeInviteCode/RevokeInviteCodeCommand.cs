using Barnabas.Application.Common.Authorisation;
using Barnabas.Domain.Members;
using MediatR;

namespace Barnabas.Application.Joining.RevokeInviteCode;

/// <summary>
/// Withdraws a code a moderator no longer wants honoured.
/// </summary>
/// <remarks>
/// Read through the filtered set, so another congregation's code is simply not there — which is
/// the whole of <c>L2-090 AC2</c>, written as an absence rather than as a comparison a handler
/// could forget.
/// </remarks>
public sealed record RevokeInviteCodeCommand(Guid InviteCodeId) : IRequest, IRequireRole
{
    public MemberRole RequiredRole => MemberRole.Moderator;
}
