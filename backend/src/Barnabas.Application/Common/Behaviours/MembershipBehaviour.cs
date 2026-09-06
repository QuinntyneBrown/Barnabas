using Barnabas.Application.Common.Authorisation;
using Barnabas.Application.Common.Tenancy;
using Barnabas.Domain.Common;
using Barnabas.Domain.Members;
using MediatR;

namespace Barnabas.Application.Common.Behaviours;

/// <summary>
/// Keeps a member who has not been let in off the board.
/// </summary>
/// <remarks>
/// One place rather than a check in every handler, for the same reason authorisation is one
/// place: a rule a handler can forget is a rule that will eventually be forgotten. It runs after
/// validation and before authorisation, so a malformed request is still answered as malformed.
/// <para>
/// It refuses rather than failing authentication, because <c>L2-011 AC1</c> asks for 403 — the
/// caller is who they say they are, and is not allowed in yet. Failing the token would say 401,
/// which reads as "sign in again" and would send them round a loop they cannot leave.
/// </para>
/// <para>
/// The status comes from the member record, restamped onto the principal each request, so a
/// moderator's approval takes effect on the next visit rather than the next sign-in.
/// </para>
/// </remarks>
public sealed class MembershipBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ICongregationContext _congregation;

    public MembershipBehaviour(ICongregationContext congregation) => _congregation = congregation;

    public Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        // Anonymous requests pass through. Joining and signing in happen before anybody has a
        // status at all, and the routes that allow them say so for themselves.
        if (!_congregation.IsResolved || request is IAllowUnapproved)
        {
            return next(cancellationToken);
        }

        if (_congregation.Status != MemberStatus.Approved)
        {
            throw new ForbiddenException();
        }

        return next(cancellationToken);
    }
}
