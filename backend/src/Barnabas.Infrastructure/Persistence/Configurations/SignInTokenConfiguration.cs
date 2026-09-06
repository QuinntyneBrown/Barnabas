using Barnabas.Domain.Access;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Barnabas.Infrastructure.Persistence.Configurations;

public sealed class SignInTokenConfiguration : IEntityTypeConfiguration<SignInToken>
{
    public void Configure(EntityTypeBuilder<SignInToken> builder)
    {
        builder.ToTable("SignInTokens");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.CongregationId).IsRequired();
        builder.Property(t => t.MemberId).IsRequired();

        // Only the hash is stored, so a database disclosure yields nothing usable: the token
        // itself exists only in the email and the URL the member follows.
        builder.Property(t => t.TokenHash).IsRequired().HasMaxLength(64);
        builder.HasIndex(t => t.TokenHash).IsUnique();
    }
}
