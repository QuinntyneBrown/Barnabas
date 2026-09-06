using Barnabas.Domain.Moderation;
using FluentValidation;

namespace Barnabas.Application.Moderation.ReportListing;

public sealed class ReportListingCommandValidator : AbstractValidator<ReportListingCommand>
{
    public ReportListingCommandValidator()
    {
        RuleFor(command => command.ListingId).NotEmpty();

        RuleFor(command => command.Reason)
            .IsInEnum()
            .WithMessage("Choose one of the reasons offered.");

        RuleFor(command => command.Note)
            .MaximumLength(ListingReport.NoteMaxLength);
    }
}
