using Barnabas.Application.Common.Exceptions;
using Barnabas.Application.Common.Persistence;
using Barnabas.Domain.Members;
using MediatR;

namespace Barnabas.Application.Congregations.DesignateModerator;

/// <summary>Grants the role and commits.</summary>
/// <remarks>
/// The member is read by the pair, so a member of another congregation is simply not found — the
/// same 404 a member who does not exist gets, which is what stops this confirming who belongs
/// where.
/// </remarks>
public sealed class DesignateModeratorCommandHandler
    : IRequestHandler<DesignateModeratorCommand, DesignatedModeratorResult>
{
    private readonly IProvisioningStore _provisioning;

    public DesignateModeratorCommandHandler(IProvisioningStore provisioning) => _provisioning = provisioning;

    public async Task<DesignatedModeratorResult> Handle(
        DesignateModeratorCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var member = await _provisioning.FindMemberAsync(
            request.CongregationId,
            request.MemberId,
            cancellationToken) ?? throw new NotFoundException();

        member.GrantRole(MemberRole.Moderator);

        await _provisioning.SaveAsync(cancellationToken);

        return new DesignatedModeratorResult(member.Id, member.Role);
    }
}
