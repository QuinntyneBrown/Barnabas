using Barnabas.Domain.Messaging;
using FluentValidation;

namespace Barnabas.Application.Messaging.SendMessage;

/// <summary>
/// The bounds a message accepts.
/// </summary>
/// <remarks>
/// These run before the handler, so a message that fails them is never appended - which is what
/// L2-067 asks for, and what the screen relies on when it keeps what the member typed.
/// </remarks>
public sealed class SendMessageCommandValidator : AbstractValidator<SendMessageCommand>
{
    public SendMessageCommandValidator()
    {
        RuleFor(command => command.Body)
            .NotEmpty()
            .MaximumLength(Message.BodyMaxLength)
            .WithMessage($"Write a message of at most {Message.BodyMaxLength} characters.");
    }
}
