namespace Barnabas.IntegrationTests.Fixtures;

/// <summary>A well-formed request to borrow, so each test varies only what it is about.</summary>
public static class Requests
{
    public static object ToBorrow(
        string? message = null,
        string? pickupOn = null,
        string? returnBy = null,
        bool acknowledged = true) => new
        {
            message = message ?? "Painting the back bedroom the weekend after next.",
            pickupOn = pickupOn ?? "2026-09-13",
            returnBy = returnBy ?? "2026-09-20",
            loanAcknowledged = acknowledged,
        };
}
