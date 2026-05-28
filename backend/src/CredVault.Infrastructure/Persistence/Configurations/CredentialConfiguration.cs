using CredVault.Domain.Credentials;
using CredVault.Infrastructure.Persistence.Conversions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CredVault.Infrastructure.Persistence.Configurations;

internal sealed class CredentialConfiguration : IEntityTypeConfiguration<Credential>
{
    public void Configure(EntityTypeBuilder<Credential> builder)
    {
        builder.ToTable("Credentials");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasDefaultValueSql("NEWSEQUENTIALID()").ValueGeneratedNever();

        builder.Property(c => c.SupplierId);
        builder.Property(c => c.EnvironmentId);
        builder.Property(c => c.Name).HasMaxLength(100).IsRequired();
        builder.Property(c => c.Slug)
            .HasConversion(ValueObjectConverters.Slug)
            .HasMaxLength(Slug.MaxLength)
            .IsRequired();

        builder.Property(c => c.CreatedAtUtc).HasColumnType("datetime2(7)");
        builder.Property(c => c.RotatedAtUtc).HasColumnType("datetime2(7)");
        builder.Property(c => c.ExpiresAtUtc).HasColumnType("datetime2(7)");

        builder.Property(c => c.EncryptedPayload).HasColumnType("varbinary(max)").IsRequired();
        builder.Property(c => c.WrappedDataKey).HasColumnType("varbinary(max)").IsRequired();
        builder.Property(c => c.Nonce).HasColumnType("varbinary(12)").IsRequired();
        builder.Property(c => c.AuthTag).HasColumnType("varbinary(16)").IsRequired();
        builder.Property(c => c.KekVersion);

        builder.Property(c => c.CredentialSchemaVersion);
        builder.Property(c => c.LastAccessedAtUtc).HasColumnType("datetime2(7)");
        builder.Property(c => c.AccessCount);
        builder.Property(c => c.MaskedPreview).HasMaxLength(16).IsRequired();

        builder.Property(c => c.IsRevoked);
        builder.Property(c => c.RevokedAtUtc).HasColumnType("datetime2(7)");
        builder.Property(c => c.RevokedByUserId);

        // OrganizationId is denormalised onto the row to support the (Org, ExpiresAt) index
        // without a join. Maintained by the application layer at write time.
        builder.Property<Guid>("OrganizationId");
        builder.HasIndex("OrganizationId", nameof(Credential.ExpiresAtUtc))
            .HasFilter("[ExpiresAtUtc] IS NOT NULL")
            .HasDatabaseName("IX_Credentials_OrgId_ExpiresAt");

        builder.HasIndex(c => new { c.EnvironmentId, c.SupplierId, c.Slug }).IsUnique();

        // Optimistic concurrency. We use a non-concurrency-token Timestamp column so EF tracks the
        // column for cache/staleness without failing the row count check (the rotate flow modifies
        // the parent and inserts a child in one SaveChanges, which makes the rowversion check
        // unreliable without further work — Phase 4 will revisit).
        builder.Property<byte[]>("RowVersion")
            .HasColumnType("rowversion")
            .ValueGeneratedOnAddOrUpdate();

        builder.HasMany(c => c.Rotations)
            .WithOne()
            .HasForeignKey(r => r.CredentialId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(Credential.Rotations))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(c => c.DomainEvents);
    }
}
