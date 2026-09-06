using Barnabas.Application.Common.Authorisation;
using Barnabas.Domain.Members;
using MediatR;

namespace Barnabas.Application.Congregations.DesignateModerator;

/// <summary>
/// Grants a member the moderator role within their own congregation.
/// </summary>
/// <remarks>
/// The congregation is named rather than taken from the caller's context, because an
/// administrator belongs to the platform congregation and is granting a role in a parish. Both
/// identifiers are required, and the store that reads the member takes the pair — so this cannot
/// be used to reach a member by identifier alone.
/// </remarks>
public sealed record DesignateModeratorCommand(Guid CongregationId, Guid MemberId)
    : IRequest<DesignatedModeratorResult>, IRequireRole
{
    public MemberRole RequiredRole => MemberRole.Administrator;
}
