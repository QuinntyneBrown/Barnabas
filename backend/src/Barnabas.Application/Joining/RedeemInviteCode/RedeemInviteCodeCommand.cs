using MediatR;

namespace Barnabas.Application.Joining.RedeemInviteCode;

/// <summary>
/// Spends a code and begins the joining flow.
/// </summary>
/// <remarks>
/// Anonymous. Nobody is signed in and no congregation is in scope; the code is what reveals which
/// congregation is being joined, which is why this reads through the narrow invitation store
/// rather than the gated set.
/// </remarks>
public sealed record RedeemInviteCodeCommand(string Code) : IRequest<RedeemedInviteCodeResult>;
