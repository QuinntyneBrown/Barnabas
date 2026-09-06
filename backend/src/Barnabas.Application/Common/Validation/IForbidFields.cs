namespace Barnabas.Application.Common.Validation;

/// <summary>
/// Declares the field names a command rejects outright for its kind.
/// </summary>
/// <remarks>
/// Unknown and forbidden are not the same thing. A field the command does not recognise is
/// ignored, so a client sending a superseded field is not broken by a deployment. A field
/// the command explicitly forbids is rejected with 400 naming it.
/// <para>
/// The distinction is load-bearing rather than pedantic. L2-027 requires a Lend listing
/// submitted with a price to be <em>rejected</em>, and <c>PostLendListingCommand</c> has no
/// <c>Price</c> property to bind one to — so if forbidden fields were merely unknown, the
/// price would be silently dropped and the response would be 201. The requirement would be
/// unimplementable by construction.
/// </para>
/// </remarks>
public interface IForbidFields
{
    /// <summary>Field names, as they appear in the JSON payload.</summary>
    static abstract IReadOnlySet<string> ForbiddenFields { get; }
}
