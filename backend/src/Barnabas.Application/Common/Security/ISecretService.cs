namespace Barnabas.Application.Common.Security;

/// <summary>
/// Creates the opaque secrets that sign-in links and refresh tokens are made of, and hashes
/// them for storage.
/// </summary>
/// <remarks>
/// Only hashes are ever persisted. A database disclosure therefore yields nothing usable: the
/// secret itself exists only in the email, the URL, or the cookie the member holds.
/// </remarks>
public interface ISecretService
{
    string CreateSecret();

    string Hash(string secret);
}
