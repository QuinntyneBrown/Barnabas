namespace Barnabas.Domain.Requests;

/// <summary>
/// Raised when a decision is applied to a request that already holds one.
/// </summary>
/// <remarks>Mapped to 409 Conflict.</remarks>
public sealed class RequestAlreadyDecidedException : Exception
{
    public RequestAlreadyDecidedException(Guid requestId, RequestStatus status)
        : base("The request has already been decided.")
    {
        RequestId = requestId;
        Status = status;
    }

    public Guid RequestId { get; }

    public RequestStatus Status { get; }
}
