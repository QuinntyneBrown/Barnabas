namespace Barnabas.Infrastructure.Email;

/// <summary>Where a sign-in link points.</summary>
public sealed class SignInLinkOptions
{
    public const string SectionName = "SignInLink";

    /// <summary>
    /// The web client's sign-in landing route. <c>{token}</c> is replaced with the secret.
    /// </summary>
    public string UrlTemplate { get; set; } = "http://localhost:4200/sign-in/{token}";
}
