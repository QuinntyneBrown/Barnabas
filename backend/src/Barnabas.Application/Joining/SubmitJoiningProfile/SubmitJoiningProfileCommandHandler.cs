using Barnabas.Application.Common.Exceptions;
using Barnabas.Application.Common.Persistence;
using Barnabas.Application.Common.Security;
using Barnabas.Domain.Congregations;
using Barnabas.Domain.Members;
using MediatR;

namespace Barnabas.Application.Joining.SubmitJoiningProfile;

/// <summary>
/// Creates the member, awaiting a moderator.
/// </summary>
/// <remarks>
/// The capability is spent before the member is created, by a conditional update whose row count
/// decides — so two profiles submitted against one entitlement produce one member rather than two.
/// <para>
/// Validation runs before any of this, in the pipeline, so an invalid field leaves the capability
/// unspent and the member can correct it and try again. That is deliberate: burning the
/// entitlement on a typo would send somebody back to a moderator for a fresh code.
/// </para>
/// </remarks>
public sealed class SubmitJoiningProfileCommandHandler
    : IRequestHandler<SubmitJoiningProfileCommand, JoinedCongregationResult>
{
    private readonly IInvitationStore _invitations;
    private readonly ISecretService _secrets;
    private readonly TimeProvider _time;

    public SubmitJoiningProfileCommandHandler(
        IInvitationStore invitations,
        ISecretService secrets,
        TimeProvider time)
    {
        _invitations = invitations;
        _secrets = secrets;
        _time = time;
    }

    public async Task<JoinedCongregationResult> Handle(
        SubmitJoiningProfileCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var now = _time.GetUtcNow();
        var hash = _secrets.Hash(request.JoiningToken);

        // Checked before the capability is spent, so a duplicate address does not cost somebody
        // their code. The unique index still has the last word if two people race.
        if (await _invitations.EmailIsRegisteredAsync(request.EmailAddress, cancellationToken))
        {
            throw new EmailAlreadyRegisteredException();
        }

        var session = await _invitations.TryConsumeJoiningSessionAsync(hash, now, cancellationToken)
            ?? throw new JoiningSessionNotUsableException();

        if (now >= session.ExpiresAt)
        {
            throw new JoiningSessionNotUsableException();
        }

        var congregation = await _invitations.FindCongregationAsync(session.CongregationId, cancellationToken)
            ?? throw new NotFoundException();

        if (!congregation.HasNeighbourhood(request.Neighbourhood))
        {
            throw new NeighbourhoodNotOfferedException(request.Neighbourhood);
        }

        var member = Member.Join(
            Guid.NewGuid(),
            congregation.Id,
            request.EmailAddress,
            request.DisplayName,
            request.Neighbourhood,
            request.ReasonForJoining);

        await _invitations.AddMemberAsync(member, cancellationToken);

        return new JoinedCongregationResult(member.Id, congregation.Name, member.Status.ToString());
    }
}
