using Barnabas.Domain.Congregations;
using Barnabas.Domain.Members;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Barnabas.Infrastructure.Persistence.Configurations;

public sealed class MemberConfiguration : IEntityTypeConfiguration<Member>
{
    public void Configure(EntityTypeBuilder<Member> builder)
    {
        builder.ToTable("Members");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.CongregationId).IsRequired();
        builder.Property(m => m.EmailAddress).IsRequired().HasMaxLength(320);
        builder.Property(m => m.DisplayName).IsRequired().HasMaxLength(200);
        builder.Property(m => m.Neighbourhood).IsRequired().HasMaxLength(200);
        builder.Property(m => m.Role).HasConversion<int>();
        builder.Property(m => m.Status).HasConversion<int>();
        builder.Property(m => m.ReasonForJoining).HasMaxLength(Member.ReasonForJoiningMaxLength);
        builder.Property(m => m.Description).HasMaxLength(Member.DescriptionMaxLength);

        // A child table rather than a delimited column, because L2-077 searches the directory by
        // tag and a LIKE over a delimited column would match "Rides" inside "Joyrides".
        builder.OwnsMany(m => m.HelpTags, tags =>
        {
            tags.ToTable("MemberHelpTags");
            tags.WithOwner().HasForeignKey(t => t.MemberId);
            tags.HasKey(t => new { t.MemberId, t.Tag });
            tags.Property(t => t.Tag).HasMaxLength(Congregation.HelpTagMaxLength);
            tags.HasIndex(t => t.Tag);
        });

        // The directory reads approved members of one congregation, by name.
        builder.HasIndex(m => new { m.CongregationId, m.Status, m.DisplayName });

        // Globally unique, not unique per congregation. Signing in resolves a member from an
        // address alone, before any congregation is known, so two members sharing an address
        // would make that lookup ambiguous at exactly the moment it cannot ask for help.
        builder.HasIndex(m => m.EmailAddress).IsUnique();
        builder.HasIndex(m => m.CongregationId);
    }
}
