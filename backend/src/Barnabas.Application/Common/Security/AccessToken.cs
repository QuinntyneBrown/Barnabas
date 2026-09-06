namespace Barnabas.Application.Common.Security;

/// <summary>A signed access token and the moment it stops being accepted.</summary>
public sealed record AccessToken(string Value, DateTimeOffset ExpiresOn);
