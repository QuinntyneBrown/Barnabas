namespace Barnabas.Application.Common.Abuse;

/// <summary>
/// Caps how often one source may guess at invite codes.
/// </summary>
/// <remarks>
/// Failures are what is counted, not attempts. Somebody redeeming their own code correctly should
/// never meet this; somebody working through the alphabet should meet it quickly. <c>L2-099 AC2</c>.
/// </remarks>
public interface IRedemptionThrottle
{
    /// <summary>Whether this source may try again.</summary>
    bool MayTry(string source);

    /// <summary>Records that an attempt from this source came to nothing.</summary>
    void RecordFailure(string source);
}
