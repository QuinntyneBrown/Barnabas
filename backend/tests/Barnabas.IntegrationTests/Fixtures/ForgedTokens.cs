using System.Security.Claims;
using System.Text;
using Barnabas.Infrastructure.Security;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Barnabas.IntegrationTests.Fixtures;

/// <summary>
/// Tokens the API should refuse, built the way an attacker would have to build them.
/// </summary>
/// <remarks>
/// These are forged rather than mangled: correctly formed, correctly signed where the point is
/// that signing is not enough, and wrong in exactly one way each. A test that sent a random
/// string would prove only that the parser works.
/// </remarks>
public static class ForgedTokens
{
    public const string Malformed = "not.a.token";

    /// <summary>Correctly signed, but naming no session, so revocation could never be checked.</summary>
    public static string WithoutSession() => Build(BarnabasApiFactory.SigningKey, includeSession: false);

    /// <summary>Well formed and complete, but signed with a key this API does not trust.</summary>
    public static string WithWrongKey() =>
        Build("a-different-key-entirely-that-the-api-does-not-know", includeSession: true);

    private static string Build(string signingKey, bool includeSession)
    {
        var claims = new List<Claim>
        {
            new(BarnabasClaims.MemberId, Guid.NewGuid().ToString()),
            new(BarnabasClaims.CongregationId, Guid.NewGuid().ToString()),
            new(BarnabasClaims.Role, "Member"),
        };

        if (includeSession)
        {
            claims.Add(new Claim(BarnabasClaims.SessionId, Guid.NewGuid().ToString()));
        }

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = "barnabas",
            Audience = "barnabas",
            Expires = DateTime.UtcNow.AddMinutes(15),
            Subject = new ClaimsIdentity(claims),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                SecurityAlgorithms.HmacSha256),
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}
