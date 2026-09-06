namespace Barnabas.Domain.Congregations;

/// <summary>
/// One parish, and the boundary around everything its members can reach.
/// </summary>
public sealed class Congregation
{
    private Congregation()
    {
    }

    public Congregation(Guid id, string name, IReadOnlyCollection<string> neighbourhoods)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(neighbourhoods);

        Id = id;
        Name = name;
        Neighbourhoods = [.. neighbourhoods];
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// The neighbourhoods a listing in this congregation may name. Commonwealth spelling
    /// throughout, per the domain language.
    /// </summary>
    public IReadOnlyList<string> Neighbourhoods { get; private set; } = [];

    public bool HasNeighbourhood(string neighbourhood) =>
        Neighbourhoods.Contains(neighbourhood, StringComparer.OrdinalIgnoreCase);
}
