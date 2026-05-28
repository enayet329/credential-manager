using CredVault.Domain.Users;
using CredVault.Infrastructure.Persistence.Conversions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CredVault.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasDefaultValueSql("NEWSEQUENTIALID()").ValueGeneratedNever();

        builder.Property(u => u.Email)
            .HasConversion(ValueObjectConverters.Email)
            .HasMaxLength(Email.MaxLength)
            .IsRequired();
        builder.Property(u => u.PasswordHash).HasMaxLength(400).IsRequired();
        builder.Property(u => u.MfaSecretReferenceId);
        builder.Property(u => u.MfaEnabled);
        builder.Property(u => u.EmailConfirmed);
        builder.Property(u => u.LastLoginUtc).HasColumnType("datetime2(7)");
        builder.Property(u => u.FailedLoginAttempts);
        builder.Property(u => u.LockoutEndUtc).HasColumnType("datetime2(7)");

        // Unique-by-email via a computed lowered column. Stored so it can be indexed. Marked
        // IsRequired so EF doesn't add a `[EmailLowered] IS NOT NULL` filter on the unique index,
        // which SQL Server rejects on computed columns.
        builder.Property<string>("EmailLowered")
            .HasComputedColumnSql("LOWER([Email])", stored: true)
            .HasMaxLength(Email.MaxLength)
            .IsRequired();
        builder.HasIndex("EmailLowered").IsUnique();

        builder.Ignore(u => u.DomainEvents);
    }
}
