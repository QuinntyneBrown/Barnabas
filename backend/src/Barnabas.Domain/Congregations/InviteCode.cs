using Barnabas.Domain.Common;

namespace Barnabas.Domain.Congregations;

/// <summary>
/// A code a prospective member redeems to join a congregation.
/// </summary>
/// <remarks>
/// Read aloud after a service and typed in by hand, which is why the alphabet leaves out the four
/// symbols people confuse on paper — <c>I</c>, <c>O</c>, <c>0</c> and <c>1</c>. Eight characters
/// of the remaining thirty-two is forty bits: guessing is pointless and the code still fits on a
/// noticeboard. See ADR-0002.
/// <para>
/// Stored upper-cased and normalised, and looked up with no congregation in scope — redemption is
/// anonymous, and the code is what reveals which congregation is being joined.
/// </para>
/// </remarks>
public sealed class InviteCode : ITenantOwned
{
    /// <summary>
    /// The symbols a code is drawn from.
    /// </summary>
    /// <remarks>
    /// Thirty-two, so each character is exactly five bits, and none of them is a letter that reads
    /// as a digit. <c>L2-005</c> asks for an unambiguous alphabet; this is what that means in
    /// practice.
    /// </remarks>
    public const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    /// <summary>Eight characters, comfortably above the six <c>L2-005</c> requires.</summary>
    public const int Length = 8;

    private InviteCode()
    {
    }

    public InviteCode(Guid id, Guid congregationId, string code, DateTimeOffset expiresAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        Id = id;
        CongregationId = congregationId;
        Code = Normalise(code);
        ExpiresAt = expiresAt;
    }

    public Guid Id { get; private set; }

    public Guid CongregationId { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? RedeemedAt { get; private set; }

    /// <summary>When a moderator withdrew it. A revoked code is as dead as a spent one.</summary>
    public DateTimeOffset? RevokedAt { get; private set; }

    /// <summary>The moderator who issued it, so a queue of codes says whose each one is.</summary>
    public Guid? IssuedByMemberId { get; private set; }

    public DateTimeOffset IssuedAt { get; private set; }

    public bool IsRedeemable(DateTimeOffset asOf) =>
        RedeemedAt is null && RevokedAt is null && asOf < ExpiresAt;

    /// <summary>
    /// Reads a code the way a person typed it.
    /// </summary>
    /// <remarks>
    /// Internal whitespace goes as well as the surrounding kind: a code read off a card is often
    /// typed with a space in the middle, and refusing that would be refusing the right code.
    /// </remarks>
    public static string Normalise(string code) =>
        new string([.. code.Where(character => !char.IsWhiteSpace(character))]).ToUpperInvariant();

    /// <summary>Formats entropy as a code, taking five bits at a time.</summary>
    public static string Format(ReadOnlySpan<byte> entropy)
    {
        if (entropy.Length < Length)
        {
            throw new ArgumentException($"A code needs at least {Length} bytes of entropy.", nameof(entropy));
        }

        return string.Create(Length, entropy.ToArray(), (span, bytes) =>
        {
            for (var index = 0; index < span.Length; index += 1)
            {
                span[index] = Alphabet[bytes[index] % Alphabet.Length];
            }
        });
    }

    public static InviteCode Issue(
        Guid id,
        Guid congregationId,
        string code,
        Guid issuedByMemberId,
        DateTimeOffset issuedAt,
        DateTimeOffset expiresAt) =>
        new(id, congregationId, code, expiresAt)
        {
            IssuedByMemberId = issuedByMemberId,
            IssuedAt = issuedAt,
        };

    /// <summary>
    /// Marks the code used.
    /// </summary>
    /// <remarks>
    /// The rule that a code redeems exactly once is not made true here. Two people typing the same
    /// code at the same moment both find it redeemable before either commits, so a conditional
    /// update whose affected-row count decides settles it — the same idiom the sign-in token uses.
    /// This records the outcome that update chose.
    /// </remarks>
    public void Redeem(DateTimeOffset asOf) => RedeemedAt = asOf;

    /// <summary>A moderator withdraws it. It answers the same way a spent code does.</summary>
    public void Revoke(DateTimeOffset asOf) => RevokedAt = asOf;
}
