using CredVault.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CredVault.Infrastructure.Persistence.Configurations;

internal sealed class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
{
    public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
    {
        builder.ToTable("PasswordResetTokens");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasDefaultValueSql("NEWSEQUENTIALID()").ValueGeneratedNever();

        builder.Property(t => t.UserId).IsRequired();
        builder.Property(t => t.TokenHash).HasMaxLength(128).IsRequired();
        builder.Property(t => t.CreatedAtUtc).HasColumnType("datetime2(7)").IsRequired();
        builder.Property(t => t.ExpiresAtUtc).HasColumnType("datetime2(7)").IsRequired();
        builder.Property(t => t.UsedAtUtc).HasColumnType("datetime2(7)");

        // Lookup happens by token hash, so it must be unique + indexed.
        builder.HasIndex(t => t.TokenHash).IsUnique();
        builder.HasIndex(t => new { t.UserId, t.UsedAtUtc });

        builder.Ignore(t => t.DomainEvents);
    }
}
