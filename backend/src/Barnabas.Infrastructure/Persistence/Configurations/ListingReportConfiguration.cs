using Barnabas.Domain.Moderation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Barnabas.Infrastructure.Persistence.Configurations;

public sealed class ListingReportConfiguration : IEntityTypeConfiguration<ListingReport>
{
    public void Configure(EntityTypeBuilder<ListingReport> builder)
    {
        builder.ToTable("ListingReports");
        builder.HasKey(report => report.Id);
        builder.Property(report => report.CongregationId).IsRequired();
        builder.Property(report => report.ListingId).IsRequired();
        builder.Property(report => report.ReporterId).IsRequired();
        builder.Property(report => report.Reason).HasConversion<int>();
        builder.Property(report => report.Note).HasMaxLength(ListingReport.NoteMaxLength);
        builder.Property(report => report.Outcome).HasConversion<int>();

        // One report per member per listing, and no filter on it.
        //
        // The request index alongside this one is filtered to open rows, because a declined
        // request may be made again. There is no equivalent here: a member has either objected to
        // this listing or has not, and objecting a second time is the same objection. L2-080 AC2
        // is therefore decided by the database rather than by a handler reading first and writing
        // afterwards.
        builder.HasIndex(report => new { report.ListingId, report.ReporterId })
            .IsUnique()
            .HasDatabaseName("IX_ListingReports_OneReportPerMember");

        // The queue reads one congregation's open reports.
        builder.HasIndex(report => new { report.CongregationId, report.ResolvedAt });
    }
}
