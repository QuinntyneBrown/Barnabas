using Barnabas.Domain.Access;
using Barnabas.Domain.Members;

namespace Barnabas.Application.Common.Security;

/// <summary>
/// Signs the access token a member presents on every subsequent request.
/// </summary>
/// <remarks>
/// Every token carries the session it belongs to. A signature can prove the token was issued
/// by this system and not altered; it cannot prove the session behind it still stands. The
/// session identifier is what lets revocation be read as state on each request, which is what
/// <c>L2-019</c> needs and what a signature alone cannot provide.
/// </remarks>
public interface IAccessTokenIssuer
{
    AccessToken Issue(Member member, Session session);
}
