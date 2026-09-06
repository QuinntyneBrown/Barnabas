using MediatR;

namespace Barnabas.Application.Messaging.GetMyThreads;

/// <summary>Asks for the threads the caller is party to.</summary>
public sealed record GetMyThreadsQuery : IRequest<IReadOnlyList<ThreadSummaryDto>>;
