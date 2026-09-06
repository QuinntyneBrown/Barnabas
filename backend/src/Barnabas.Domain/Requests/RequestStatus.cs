namespace Barnabas.Domain.Requests;

/// <summary>
/// The states a request holds. A request is decided once.
/// </summary>
public enum RequestStatus
{
    Pending = 0,
    Accepted = 1,
    Declined = 2,
}
