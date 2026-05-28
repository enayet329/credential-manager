using CredVault.Domain.Credentials;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CredVault.Infrastructure.Persistence.Configurations;

internal sealed class CredentialRotationConfiguration : IEntityTypeConfiguration<CredentialRotation>
{
    public void Configure(EntityTypeBuilder<CredentialRotation> builder)
    {
        builder.ToTable("CredentialRotations");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasDefaultValueSql("NEWSEQUENTIALID()").ValueGeneratedNever();

        builder.Property(r => r.CredentialId);
        builder.Property(r => r.RotatedAtUtc).HasColumnType("datetime2(7)");
        builder.Property(r => r.RotatedByUserId);

        builder.Property(r => r.PreviousEncryptedPayload).HasColumnType("varbinary(max)").IsRequired();
        builder.Property(r => r.PreviousWrappedDataKey).HasColumnType("varbinary(max)").IsRequired();
        builder.Property(r => r.PreviousNonce).HasColumnType("varbinary(12)").IsRequired();
        builder.Property(r => r.PreviousAuthTag).HasColumnType("varbinary(16)").IsRequired();
        builder.Property(r => r.PreviousKekVersion);
        builder.Property(r => r.Reason).HasMaxLength(500);

        builder.HasIndex(r => new { r.CredentialId, r.RotatedAtUtc });

        builder.Ignore(r => r.DomainEvents);
    }
}
