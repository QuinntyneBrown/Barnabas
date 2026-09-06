using System.Security.Claims;
using System.Text;
using Barnabas.Application.Common.Security;
using Barnabas.Domain.Access;
using Barnabas.Domain.Members;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Barnabas.Infrastructure.Security;

/// <inheritdoc />
public sealed class JwtIssuer : IAccessTokenIssuer
{
    private readonly JwtOptions _options;
    private readonly TimeProvider _time;

    public JwtIssuer(IOptions<JwtOptions> options, TimeProvider time)
    {
        _options = options.Value;
        _time = time;
    }

    public AccessToken Issue(Member member, Session session)
    {
        ArgumentNullException.ThrowIfNull(member);
        ArgumentNullException.ThrowIfNull(session);

        var issuedAt = _time.GetUtcNow();
        var expiresOn = issuedAt + _options.AccessTokenLifetime;

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            IssuedAt = issuedAt.UtcDateTime,
            NotBefore = issuedAt.UtcDateTime,
            Expires = expiresOn.UtcDateTime,
            Subject = new ClaimsIdentity(
            [
                new Claim(BarnabasClaims.MemberId, member.Id.ToString()),

                // Taken from the member record the authentication store returned, never from
                // anything the caller supplied. Altering it invalidates the signature, which is
                // what L2-088 relies on.
                new Claim(BarnabasClaims.CongregationId, member.CongregationId.ToString()),
                new Claim(BarnabasClaims.Role, member.Role.ToString()),
                new Claim(BarnabasClaims.SessionId, session.Id.ToString()),
            ]),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey)),
                SecurityAlgorithms.HmacSha256),
        };

        return new AccessToken(new JsonWebTokenHandler().CreateToken(descriptor), expiresOn);
    }
}
