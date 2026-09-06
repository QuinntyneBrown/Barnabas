namespace Barnabas.Application.Joining.Common;

/// <summary>
/// How long an invite code and a joining session last.
/// </summary>
/// <remarks>
/// Options rather than constants, because these are properties of a deployment rather than of
/// what a code is. A parish that treats a code as a one-evening thing changes configuration
/// instead of code. The values and the reasoning are in ADR-0002.
/// </remarks>
public sealed class InviteCodeOptions
{
    public const string Section = "Invitations";

    /// <summary>
    /// Long enough for a code printed in a bulletin, short enough that a photographed
    /// noticeboard stops working.
    /// </summary>
    public TimeSpan Lifetime { get; set; } = TimeSpan.FromDays(30);

    /// <summary>
    /// How long somebody has to finish joining after redeeming.
    /// </summary>
    /// <remarks>
    /// Matches the sign-in link deliberately. Both are single-use credentials held by somebody the
    /// system does not yet know, and neither should outlive the sitting in which it was issued.
    /// </remarks>
    public TimeSpan JoiningSessionLifetime { get; set; } = TimeSpan.FromMinutes(15);
}
