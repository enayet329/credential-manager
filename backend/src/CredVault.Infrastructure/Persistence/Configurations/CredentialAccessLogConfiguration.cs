using CredVault.Domain.Credentials;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CredVault.Infrastructure.Persistence.Configurations;

internal sealed class CredentialAccessLogConfiguration : IEntityTypeConfiguration<CredentialAccessLog>
{
    public void Configure(EntityTypeBuilder<CredentialAccessLog> builder)
    {
        builder.ToTable("CredentialAccessLogs");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).HasDefaultValueSql("NEWSEQUENTIALID()").ValueGeneratedNever();

        builder.Property(l => l.CredentialId);
        builder.Property(l => l.AccessedAtUtc).HasColumnType("datetime2(7)");
        builder.Property(l => l.ActorType).HasConversion<int>();
        builder.Property(l => l.ActorId);
        builder.Property(l => l.IpAddress).HasMaxLength(45).IsRequired();
        builder.Property(l => l.UserAgent).HasMaxLength(400).IsRequired();
        builder.Property(l => l.AccessMethod).HasConversion<int>();
        builder.Property(l => l.Outcome).HasConversion<int>();

        builder.Property<Guid>("OrganizationId");

        builder.HasIndex(l => new { l.CredentialId, l.AccessedAtUtc })
            .HasDatabaseName("IX_CredentialAccessLogs_CredentialId_AccessedAt")
            .IsDescending(false, true);

        builder.HasIndex("OrganizationId", nameof(CredentialAccessLog.AccessedAtUtc))
            .HasDatabaseName("IX_CredentialAccessLogs_OrgId_AccessedAt")
            .IsDescending(false, true);

        builder.Ignore(l => l.DomainEvents);
    }
}
