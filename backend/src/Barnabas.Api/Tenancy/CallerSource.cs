using Barnabas.Application.Common.Abuse;

namespace Barnabas.Api.Tenancy;

/// <inheritdoc />
/// <remarks>
/// The connection's remote address. Behind a proxy every caller would share one key, which
/// throttles honest members along with a guesser — so a deployment behind one has to give the
/// forwarded-headers middleware its trusted proxies before this means anything. Recorded here
/// rather than assumed away.
/// </remarks>
public sealed class CallerSource : ICallerSource
{
    private readonly IHttpContextAccessor _accessor;

    public CallerSource(IHttpContextAccessor accessor) => _accessor = accessor;

    public string Key =>
        _accessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
