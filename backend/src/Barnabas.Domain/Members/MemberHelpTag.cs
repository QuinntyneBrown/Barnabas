namespace Barnabas.Domain.Members;

/// <summary>
/// One kind of help a member has said they can offer.
/// </summary>
/// <remarks>
/// A child table rather than a delimited column, which is the opposite of how the congregation's
/// own set is stored, and deliberately so: <c>L2-077</c> searches the directory <em>by tag</em>,
/// and a `LIKE '%tag%'` over a delimited column is neither indexable nor honest — it would match
/// "Rides" inside "Joyrides".
/// <para>
/// Keyed on the pair rather than on an invented identifier. A member declares a tag once, and
/// saying so in the key is cheaper than a rule that has to be remembered.
/// </para>
/// </remarks>
public sealed class MemberHelpTag
{
    private MemberHelpTag()
    {
    }

    public MemberHelpTag(Guid memberId, string tag)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tag);

        MemberId = memberId;
        Tag = tag;
    }

    public Guid MemberId { get; private set; }

    public string Tag { get; private set; } = string.Empty;
}
