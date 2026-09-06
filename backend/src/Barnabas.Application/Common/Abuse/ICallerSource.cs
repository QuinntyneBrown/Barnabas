namespace Barnabas.Application.Common.Abuse;

/// <summary>
/// Where the current request appears to come from.
/// </summary>
/// <remarks>
/// An abstraction rather than a parameter on a command, for the same reason the congregation is:
/// it is a fact about the request rather than something a caller supplies, so there is nothing in
/// a payload to change. The API layer knows how to read it; the application layer only needs a
/// stable key to count against.
/// </remarks>
public interface ICallerSource
{
    /// <summary>A stable key for the caller, or a shared one when it cannot be told.</summary>
    string Key { get; }
}
