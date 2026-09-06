using MediatR;

namespace Barnabas.Application.Members.GetMemberProfile;

/// <summary>Another member of the caller's own congregation.</summary>
public sealed record GetMemberProfileQuery(Guid MemberId) : IRequest<MemberProfileDto>;
