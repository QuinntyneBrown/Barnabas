namespace Barnabas.Domain.Members;

/// <summary>
/// Raised when an address already belongs to somebody.
/// </summary>
/// <remarks>
/// One address is one member across the whole deployment, because sign-in looks a member up by
/// address with no congregation in scope. The message says nothing about which congregation holds
/// it: that would be an enumeration surface on an anonymous route.
/// </remarks>
public sealed class EmailAlreadyRegisteredException : Exception
{
    public EmailAlreadyRegisteredException()
        : base("That email address is already in use.")
    {
    }
}
