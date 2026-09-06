using Barnabas.Domain.Members;

namespace Barnabas.Application.Common.Authorisation;

/// <summary>
/// Declares the role a request demands.
/// </summary>
/// <remarks>
/// Role and ownership are separate markers rather than one policy, because they are
/// independent: a moderator removing a flagged listing demands a role and no ownership;
/// a member accepting a request demands ownership and no particular role.
/// <para>
/// No request in feature slice 1 implements this. It is wired anyway so that L2-095 is a
/// declaration on a command rather than a refactor of the pipeline.
/// </para>
/// </remarks>
public interface IRequireRole
{
    MemberRole RequiredRole { get; }
}
