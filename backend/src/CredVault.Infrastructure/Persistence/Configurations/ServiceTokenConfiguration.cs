using CredVault.Domain.ServiceTokens;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CredVault.Infrastructure.Persistence.Configurations;

internal sealed class ServiceTokenConfiguration : IEntityTypeConfiguration<ServiceToken>
{
    public void Configure(EntityTypeBuilder<ServiceToken> builder)
    {
        builder.ToTable("ServiceTokens");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasDefaultValueSql("NEWSEQUENTIALID()").ValueGeneratedNever();

        builder.Property(t => t.OrganizationId);
        builder.Property(t => t.ProjectId);
        builder.Property(t => t.Prefix).HasMaxLength(32).IsRequired();
        builder.Property(t => t.HmacHash).HasColumnType("varbinary(64)").IsRequired();
        builder.Property(t => t.Label).HasMaxLength(100).IsRequired();

        builder.Property(t => t.CreatedAtUtc).HasColumnType("datetime2(7)");
        builder.Property(t => t.LastUsedAtUtc).HasColumnType("datetime2(7)");
        builder.Property(t => t.ExpiresAtUtc).HasColumnType("datetime2(7)");
        builder.Property(t => t.RevokedAtUtc).HasColumnType("datetime2(7)");
        builder.Property(t => t.CreatedByUserId);

        builder.HasIndex(t => t.HmacHash).IsUnique();
        builder.HasIndex(t => new { t.OrganizationId, t.RevokedAtUtc });

        builder.Property<byte[]>("RowVersion").IsRowVersion();

        builder.Ignore(t => t.DomainEvents);
    }
}
