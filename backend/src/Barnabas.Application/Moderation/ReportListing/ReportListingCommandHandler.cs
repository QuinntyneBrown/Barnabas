using Barnabas.Application.Common.Exceptions;
using Barnabas.Application.Common.Persistence;
using Barnabas.Application.Common.Tenancy;
using Barnabas.Domain.Moderation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Barnabas.Application.Moderation.ReportListing;

/// <summary>
/// Records the complaint and marks the listing for a moderator.
/// </summary>
/// <remarks>
/// The listing is read through the filtered set, so one in another congregation is simply absent
/// and the answer is 404.
/// <para>
/// The flag is a mark on the listing rather than a status, so the listing stays exactly where it
/// was. A complaint is not a verdict: nothing comes off the board until a moderator says so, and
/// <c>L2-083</c> requires an approved listing to still be there afterwards.
/// </para>
/// </remarks>
public sealed class ReportListingCommandHandler : IRequestHandler<ReportListingCommand, ReportedListingResult>
{
    private readonly IBarnabasDbContext _context;
    private readonly ICongregationContext _congregation;
    private readonly TimeProvider _time;

    public ReportListingCommandHandler(
        IBarnabasDbContext context,
        ICongregationContext congregation,
        TimeProvider time)
    {
        _context = context;
        _congregation = congregation;
        _time = time;
    }

    public async Task<ReportedListingResult> Handle(
        ReportListingCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var listing = await _context.Listings
            .FirstOrDefaultAsync(candidate => candidate.Id == request.ListingId, cancellationToken)
            ?? throw new NotFoundException();

        var reporterId = _congregation.MemberId;
        var now = _time.GetUtcNow();

        var report = new ListingReport(
            Guid.NewGuid(),
            listing.CongregationId,
            listing.Id,
            reporterId,
            request.Reason,
            string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
            now);

        _context.ListingReports.Add(report);
        listing.Flag(now);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // The unique index on (ListingId, ReporterId) is what decides this, not a read
            // beforehand: two complaints arriving together would both pass a check and only one
            // can pass the index. L2-080 AC2.
            throw new AlreadyReportedException(listing.Id);
        }

        return new ReportedListingResult(report.Id, listing.Id);
    }
}
