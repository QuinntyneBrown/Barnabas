using Barnabas.Application.Common.Authorisation;
using Barnabas.Application.Common.Validation;
using Barnabas.Domain.Members;
using MediatR;

namespace Barnabas.Application.Congregations.ProvisionCongregation;

/// <summary>
/// Brings a congregation into being, with somebody able to run it.
/// </summary>
/// <remarks>
/// The founding moderator is not in <c>L2-001</c>, and is here because without one the
/// congregation is unreachable: only a moderator may issue an invite code, so a parish
/// provisioned with no members could never gain any. Recorded in ADR-0002 rather than absorbed
/// quietly.
/// <para>
/// A submitted <c>slug</c> change is refused by name on the configure command; here the slug is
/// what is being set, so it is an ordinary field.
/// </para>
/// </remarks>
public sealed record ProvisionCongregationCommand(
    string Name,
    string Slug,
    IReadOnlyList<string>? Neighbourhoods,
    string FoundingModeratorEmail,
    string FoundingModeratorDisplayName) : IRequest<ProvisionedCongregationResult>, IRequireRole, IForbidFields
{
    public MemberRole RequiredRole => MemberRole.Administrator;

    public static IReadOnlySet<string> ForbiddenFields { get; } = PaymentDeliveryAndDepositFields.Names;
}
