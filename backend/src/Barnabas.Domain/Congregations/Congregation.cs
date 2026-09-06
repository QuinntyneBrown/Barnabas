using System.Text.RegularExpressions;

namespace Barnabas.Domain.Congregations;

/// <summary>
/// One parish, and the boundary around everything its members can reach.
/// </summary>
/// <remarks>
/// Not <c>ITenantOwned</c>. A congregation is the tenant, which is why its set is the one the
/// context does not gate.
/// </remarks>
public sealed partial class Congregation
{
    public const int SlugMinLength = 3;
    public const int SlugMaxLength = 64;
    public const int NameMaxLength = 200;
    public const int NeighbourhoodMaxLength = 100;
    public const int MaxNeighbourhoods = 50;

    private Congregation()
    {
    }

    private Congregation(Guid id, string name, string slug, IReadOnlyCollection<string> neighbourhoods)
    {
        Id = id;
        Name = name;
        Slug = slug;
        Neighbourhoods = [.. neighbourhoods];
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// The congregation's name in an address. Unique across the deployment, and immutable.
    /// </summary>
    /// <remarks>
    /// Immutable by the absence of a mutator rather than by a check. Nothing here can change it,
    /// and the command that could refuses a submitted slug by name — <c>L2-001</c>.
    /// <para>
    /// Lower-cased on the way in, which is what lets a plain unique index enforce
    /// case-insensitive uniqueness without depending on the server's collation. The same argument
    /// ADR-0001 makes about constraints behaving in a test as they will in a deployment.
    /// </para>
    /// </remarks>
    public string Slug { get; private set; } = string.Empty;

    /// <summary>
    /// The neighbourhoods a listing or a member in this congregation may name.
    /// </summary>
    /// <remarks>
    /// Ordered, because the order is how they are offered. Commonwealth spelling throughout, per
    /// the domain language.
    /// </remarks>
    public IReadOnlyList<string> Neighbourhoods { get; private set; } = [];

    public bool HasNeighbourhood(string neighbourhood) =>
        Neighbourhoods.Contains(neighbourhood, StringComparer.OrdinalIgnoreCase);

    /// <summary>The shape a slug has to have. See ADR-0002.</summary>
    public static bool IsValidSlug(string? slug) =>
        slug is not null
        && slug.Length >= SlugMinLength
        && slug.Length <= SlugMaxLength
        && SlugPattern().IsMatch(slug);

    /// <summary>Lower-cases and trims, so `St-Aidans` and `st-aidans` are the same congregation.</summary>
    public static string NormaliseSlug(string slug) => slug.Trim().ToLowerInvariant();

    public static Congregation Provision(
        Guid id,
        string name,
        string slug,
        IReadOnlyCollection<string> neighbourhoods)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(neighbourhoods);

        var normalised = NormaliseSlug(slug);

        if (!IsValidSlug(normalised))
        {
            throw new ArgumentException("A slug is 3 to 64 lower-case letters, digits and hyphens.", nameof(slug));
        }

        var congregation = new Congregation(id, name, normalised, []);

        congregation.SetNeighbourhoods(neighbourhoods);

        return congregation;
    }

    public void Rename(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name;
    }

    /// <summary>
    /// Replaces the neighbourhoods, keeping the order they were supplied in.
    /// </summary>
    /// <remarks>
    /// The order is part of the value: it is the order a member is offered them in, so a list that
    /// came back sorted would be a different answer from the one that was given.
    /// </remarks>
    public void SetNeighbourhoods(IReadOnlyCollection<string> neighbourhoods)
    {
        ArgumentNullException.ThrowIfNull(neighbourhoods);

        if (neighbourhoods.Count is 0 or > MaxNeighbourhoods)
        {
            throw new ArgumentException(
                $"A congregation names between 1 and {MaxNeighbourhoods} neighbourhoods.",
                nameof(neighbourhoods));
        }

        if (neighbourhoods.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("A neighbourhood needs a name.", nameof(neighbourhoods));
        }

        if (neighbourhoods.Any(name => name.Length > NeighbourhoodMaxLength))
        {
            throw new ArgumentException(
                $"A neighbourhood name is at most {NeighbourhoodMaxLength} characters.",
                nameof(neighbourhoods));
        }

        if (neighbourhoods.Distinct(StringComparer.OrdinalIgnoreCase).Count() != neighbourhoods.Count)
        {
            throw new ArgumentException("Each neighbourhood is named once.", nameof(neighbourhoods));
        }

        Neighbourhoods = [.. neighbourhoods];
    }

    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex SlugPattern();
}
