namespace Barnabas.Application.Members.EraseMyData;

/// <summary>What was erased, counted rather than named.</summary>
public sealed record ErasedDataResult(
    Guid MemberId,
    int ListingsErased,
    int MessagesErased,
    DateTimeOffset ErasedAt);
