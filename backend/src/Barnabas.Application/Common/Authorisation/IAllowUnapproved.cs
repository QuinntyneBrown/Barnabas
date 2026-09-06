namespace Barnabas.Application.Common.Authorisation;

/// <summary>
/// Declares that a request may be made by a member a moderator has not let in yet.
/// </summary>
/// <remarks>
/// The exception rather than the rule, and stated per request so the default is closed. A member
/// awaiting approval can sign in, read which congregation they are waiting on, sign out, and
/// nothing else — <c>L2-011</c>. Everything else is refused without each handler having to
/// remember to check.
/// </remarks>
public interface IAllowUnapproved;
