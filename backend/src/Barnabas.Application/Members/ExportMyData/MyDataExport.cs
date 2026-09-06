namespace Barnabas.Application.Members.ExportMyData;

/// <summary>
/// Everything Barnabas holds about one member, in one document.
/// </summary>
/// <remarks>
/// Their own, and only theirs. A thread has two sides and both wrote in it, so the messages here
/// are the ones this member sent — the other party's words are the other party's data, and
/// handing them over would be answering one member's rights by breaching another's.
/// </remarks>
public sealed record MyDataExport(
    DateTimeOffset ExportedAt,
    ExportedProfile Profile,
    IReadOnlyList<ExportedListing> Listings,
    IReadOnlyList<ExportedRequest> Requests,
    IReadOnlyList<ExportedMessage> Messages);
