namespace Barnabas.Domain.Requests;

/// <summary>
/// The outcome of <see cref="RequestEligibilityPolicy"/>.
/// </summary>
/// <remarks>
/// The API maps a self-request and a kind mismatch to 400, and a closed listing and a
/// duplicate to 409, as L2-062 requires.
/// </remarks>
public enum RequestEligibility
{
    Eligible = 0,

    /// <summary>The caller owns the listing. 400.</summary>
    OwnListing = 1,

    /// <summary>The endpoint does not match the listing's kind. 400.</summary>
    KindMismatch = 2,

    /// <summary>The listing has been closed out or archived. 409.</summary>
    ListingNotActive = 3,

    /// <summary>The caller already holds an open request against this listing. 409.</summary>
    DuplicateRequest = 4,
}
