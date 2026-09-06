using Barnabas.Domain.Requests;

namespace Barnabas.Application.Common.Exceptions;

/// <summary>
/// Raised when a request fails one of the four conditions it has to satisfy.
/// </summary>
/// <remarks>
/// The reason is carried rather than flattened into a status code here, because the four
/// conditions do not answer alike. Asking for your own listing, or asking through the endpoint
/// for the wrong kind, is a malformed ask: 400. Asking for something already gone, or asking
/// twice, is a well-formed ask that the state of the board refuses: 409.
/// </remarks>
public sealed class RequestNotEligibleException : Exception
{
    public RequestNotEligibleException(RequestEligibility reason)
        : base("The request cannot be made against this listing.") => Reason = reason;

    public RequestEligibility Reason { get; }
}
