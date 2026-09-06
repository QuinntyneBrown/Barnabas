using Barnabas.Domain.Members;

namespace Barnabas.Application.Moderation.ApproveMember;

/// <summary>Where an applicant stands after a moderator has decided.</summary>
public sealed record DecidedMemberResult(Guid MemberId, MemberStatus Status);
