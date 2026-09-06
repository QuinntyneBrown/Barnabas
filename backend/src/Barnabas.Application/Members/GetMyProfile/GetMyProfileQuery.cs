using Barnabas.Application.Common.Authorisation;
using MediatR;

namespace Barnabas.Application.Members.GetMyProfile;

/// <summary>
/// The caller's own profile.
/// </summary>
/// <remarks>
/// It names no member. The identifier comes from the verified session, so there is nothing here
/// to change in order to read somebody else's.
/// <para>
/// Reachable by a member still waiting on a moderator: somebody who cannot read their own
/// settings has no way to correct the address their sign-in link goes to.
/// </para>
/// </remarks>
public sealed record GetMyProfileQuery : IRequest<MyProfileDto>, IAllowUnapproved;
