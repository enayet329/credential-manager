using CredVault.Domain.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CredVault.Infrastructure.Persistence.Configurations;

internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).HasDefaultValueSql("NEWSEQUENTIALID()").ValueGeneratedNever();

        builder.Property(l => l.OrganizationId);
        builder.Property(l => l.ActorUserId);
        builder.Property(l => l.ActorServiceTokenId);
        builder.Property(l => l.ActorIpAddress).HasMaxLength(45).IsRequired();
        builder.Property(l => l.ActorUserAgent).HasMaxLength(400).IsRequired();
        builder.Property(l => l.OccurredAtUtc).HasColumnType("datetime2(7)");
        builder.Property(l => l.Action).HasMaxLength(100).IsRequired();
        builder.Property(l => l.TargetType).HasMaxLength(100).IsRequired();
        builder.Property(l => l.TargetId).HasMaxLength(100).IsRequired();
        builder.Property(l => l.Outcome).HasConversion<int>();
        builder.Property(l => l.Reason).HasMaxLength(500);

        // MetadataJson column type chosen at runtime via JsonColumnType()
        // (configured in CredVaultDbContext.OnModelCreating).
        builder.Property(l => l.MetadataJson).IsRequired();

        builder.HasIndex(l => new { l.OrganizationId, l.OccurredAtUtc })
            .HasDatabaseName("IX_AuditLogs_OrgId_OccurredAt")
            .IsDescending(false, true);

        builder.Ignore(l => l.DomainEvents);
    }
}
