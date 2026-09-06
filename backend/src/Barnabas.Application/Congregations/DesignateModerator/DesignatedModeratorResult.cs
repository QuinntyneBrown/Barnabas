using Barnabas.Domain.Members;

namespace Barnabas.Application.Congregations.DesignateModerator;

public sealed record DesignatedModeratorResult(Guid MemberId, MemberRole Role);
