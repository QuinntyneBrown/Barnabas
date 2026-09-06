namespace Barnabas.Application.Common.Exceptions;

/// <summary>
/// Raised when a caller has asked for something oftener than they may.
/// </summary>
/// <remarks>
/// Carries how long to wait, because a 429 that does not say when to come back leaves a client
/// guessing and usually retrying sooner. <c>L2-099 AC1</c> asks for the header.
/// </remarks>
public sealed class TooManyRequestsException : Exception
{
    public TooManyRequestsException()
        : this(TimeSpan.FromMinutes(15))
    {
    }

    public TooManyRequestsException(TimeSpan retryAfter)
        : base("That has been asked for too often. Try again shortly.") => RetryAfter = retryAfter;

    public TimeSpan RetryAfter { get; }
}
