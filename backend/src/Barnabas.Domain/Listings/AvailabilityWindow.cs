namespace Barnabas.Domain.Listings;

/// <summary>
/// One period a Help listing's owner is free, on a recurring day of the week.
/// </summary>
/// <remarks>
/// Help offers time, and time has to be offered when the member is actually available, so a
/// Help listing declares its windows and a request chooses one of them.
/// <para>
/// This carries an identifier where <see cref="LoanTerms"/> does not, and the difference is the
/// whole reason it is an entity rather than a value: a request has to name the window it wants,
/// and two windows that happen to hold the same day and hours are still two different offers.
/// A value type would make them indistinguishable.
/// </para>
/// <para>
/// The times are local to the congregation and no zone is stored. Members of one parish arrange
/// a lift between themselves in the hours they both recognise; carrying an offset would imply a
/// precision the offer does not have.
/// </para>
/// </remarks>
public sealed class AvailabilityWindow
{
    private AvailabilityWindow()
    {
    }

    public AvailabilityWindow(Guid id, DayOfWeek day, TimeOnly startsAt, TimeOnly endsAt)
    {
        if (endsAt <= startsAt)
        {
            throw new ArgumentOutOfRangeException(
                nameof(endsAt),
                endsAt,
                "A window has to end after it starts.");
        }

        Id = id;
        Day = day;
        StartsAt = startsAt;
        EndsAt = endsAt;
    }

    public Guid Id { get; private set; }

    public DayOfWeek Day { get; private set; }

    public TimeOnly StartsAt { get; private set; }

    public TimeOnly EndsAt { get; private set; }
}
