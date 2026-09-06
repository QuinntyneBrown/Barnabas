namespace Barnabas.Application.Joining.IssueInviteCode;

/// <summary>The code as the moderator has to read it out, and when it stops working.</summary>
public sealed record IssuedInviteCodeResult(Guid InviteCodeId, string Code, DateTimeOffset ExpiresAt);
