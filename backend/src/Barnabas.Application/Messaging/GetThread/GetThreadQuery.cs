using MediatR;

namespace Barnabas.Application.Messaging.GetThread;

/// <summary>Opens one thread, and marks it read for the member opening it.</summary>
public sealed record GetThreadQuery(Guid ThreadId) : IRequest<ThreadDetailDto>;
