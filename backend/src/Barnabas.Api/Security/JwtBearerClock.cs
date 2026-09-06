using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;

namespace Barnabas.Api.Security;

/// <summary>
/// Validates token lifetimes against the application's clock rather than the machine's.
/// </summary>
/// <remarks>
/// The bearer handler reads the system clock by default, which would leave every rule about
/// elapsed time untestable except by waiting: an acceptance test for "a token past its expiry is
/// refused" would have to sleep for the whole access-token lifetime, and one for a sign-in link
/// would have to sleep for sixteen minutes.
/// <para>
/// Reading the injected <see cref="TimeProvider"/> instead costs nothing in production, where it
/// is the system clock, and makes L2-015 and L2-018 assertable in milliseconds.
/// </para>
/// </remarks>
public sealed class JwtBearerClock : IPostConfigureOptions<JwtBearerOptions>
{
    private readonly TimeProvider _time;

    public JwtBearerClock(TimeProvider time) => _time = time;

    public void PostConfigure(string? name, JwtBearerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.TokenValidationParameters.LifetimeValidator = (notBefore, expires, _, _) =>
        {
            var now = _time.GetUtcNow().UtcDateTime;

            // No leeway in either direction. L2-018 says a token past its expiry is refused, and
            // the handler's usual five minutes of skew would make that false for five minutes.
            if (notBefore is not null && now < notBefore.Value)
            {
                return false;
            }

            return expires is null || now < expires.Value;
        };
    }
}
