namespace Barnabas.Application.Search.SearchListings;

/// <summary>
/// A page of results, with the term that produced them.
/// </summary>
/// <remarks>
/// The term travels back so the screen can state what was searched without holding it — which is
/// what <c>L2-052</c> asks for on a search that found nothing, when there is nothing else on the
/// page to say what happened.
/// </remarks>
public sealed record SearchPage(string Term, IReadOnlyList<SearchResultDto> Results, string? NextCursor);
