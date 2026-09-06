namespace Barnabas.Application.Members.ExportMyData;

/// <summary>One request this member made against somebody else's listing.</summary>
public sealed record ExportedRequest(
    Guid RequestId,
    Guid ListingId,
    string ListingTitle,
    string Message,
    string Status,
    DateTimeOffset MadeAt);
