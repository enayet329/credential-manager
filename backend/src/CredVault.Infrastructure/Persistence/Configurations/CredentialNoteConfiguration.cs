using CredVault.Domain.Credentials;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CredVault.Infrastructure.Persistence.Configurations;

internal sealed class CredentialNoteConfiguration : IEntityTypeConfiguration<CredentialNote>
{
    public void Configure(EntityTypeBuilder<CredentialNote> builder)
    {
        builder.ToTable("CredentialNotes");
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).HasDefaultValueSql("NEWSEQUENTIALID()").ValueGeneratedNever();

        builder.Property(n => n.CredentialId);
        builder.Property(n => n.CreatedAtUtc).HasColumnType("datetime2(7)");
        builder.Property(n => n.CreatedByUserId);

        builder.Property(n => n.EncryptedContent).HasColumnType("varbinary(max)").IsRequired();
        builder.Property(n => n.WrappedDataKey).HasColumnType("varbinary(max)").IsRequired();
        builder.Property(n => n.Nonce).HasColumnType("varbinary(12)").IsRequired();
        builder.Property(n => n.AuthTag).HasColumnType("varbinary(16)").IsRequired();
        builder.Property(n => n.KekVersion);

        builder.HasIndex(n => n.CredentialId);

        builder.Ignore(n => n.DomainEvents);
    }
}
