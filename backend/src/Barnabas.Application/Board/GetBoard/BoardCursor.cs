using System.Globalization;
using System.Text;

namespace Barnabas.Application.Board.GetBoard;

/// <summary>
/// Where the previous page stopped.
/// </summary>
/// <remarks>
/// A cursor rather than an offset. Listings are posted while a member is reading, and an offset
/// would quietly skip or repeat a row every time one arrived. The identifier is carried
/// alongside the timestamp because two listings can be posted in the same instant, and the pair
/// is what makes the order total.
/// <para>
/// It is opaque to the client by encoding rather than by encryption: it names a position in a
/// list the caller is already allowed to read, so there is nothing in it to protect.
/// </para>
/// </remarks>
public sealed record BoardCursor(DateTimeOffset PostedAt, Guid ListingId)
{
    public string Encode() =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(
            $"{PostedAt.UtcTicks.ToString(CultureInfo.InvariantCulture)}:{ListingId}"));

    public static BoardCursor? Decode(string? cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor))
        {
            return null;
        }

        try
        {
            var parts = Encoding.UTF8.GetString(Convert.FromBase64String(cursor)).Split(':');

            if (parts.Length != 2
                || !long.TryParse(parts[0], CultureInfo.InvariantCulture, out var ticks)
                || !Guid.TryParse(parts[1], out var listingId))
            {
                return null;
            }

            return new BoardCursor(new DateTimeOffset(ticks, TimeSpan.Zero), listingId);
        }
        catch (FormatException)
        {
            // A cursor that will not decode is treated as no cursor at all. The member gets the
            // first page rather than an error about a value they never typed.
            return null;
        }
    }
}
