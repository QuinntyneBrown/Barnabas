using Barnabas.Application.Common.Authorisation;
using Barnabas.Application.Common.Validation;
using Barnabas.Domain.Members;
using MediatR;

namespace Barnabas.Application.Congregations.ConfigureCongregation;

/// <summary>
/// Renames a congregation and sets the neighbourhoods it offers.
/// </summary>
/// <remarks>
/// The slug is refused by name rather than ignored. It is how the congregation is named in an
/// address, and <c>L2-001 AC3</c> requires an attempt to change it to be reported.
/// </remarks>
public sealed record ConfigureCongregationCommand(
    Guid CongregationId,
    string Name,
    IReadOnlyList<string>? Neighbourhoods) : IRequest<ConfiguredCongregationResult>, IRequireRole, IForbidFields
{
    public MemberRole RequiredRole => MemberRole.Administrator;

    public static IReadOnlySet<string> ForbiddenFields { get; } =
        PaymentDeliveryAndDepositFields.And("slug");
}
