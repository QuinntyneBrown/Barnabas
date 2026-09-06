namespace Barnabas.Budgets;

/// <summary>
/// What a measurement is compared against, and where the number came from.
/// </summary>
/// <remarks>
/// The budgets are read from configuration rather than written into the assertions, so a
/// deployment on slower hardware can state its own and still be honest about it — a number
/// somebody edited in a source file is a number nobody notices changing.
/// </remarks>
public sealed record Budget(string Name, string Criterion, double MaximumMilliseconds);

/// <summary>One measurement, with everything a reader needs to judge it.</summary>
public sealed record Measurement(Budget Budget, int Samples, double P50, double P95, double Max)
{
    public bool Met => P95 <= Budget.MaximumMilliseconds;

    /// <summary>One line of the report. The numbers first, then whether they were good enough.</summary>
    public override string ToString() =>
        $"{(Met ? "  met" : "MISSED")}  {Budget.Criterion,-12} {Budget.Name,-38} "
        + $"n={Samples,-5} p50={P50,7:0.0}ms  p95={P95,7:0.0}ms  max={Max,7:0.0}ms  "
        + $"budget={Budget.MaximumMilliseconds:0}ms";
}
