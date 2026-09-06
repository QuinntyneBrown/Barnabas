using Barnabas.Application.Messaging.GetThread;
using MediatR;

namespace Barnabas.Application.Messaging.SendMessage;

/// <summary>Appends a message to a thread.</summary>
public sealed record SendMessageCommand(Guid ThreadId, string Body) : IRequest<MessageDto>;
