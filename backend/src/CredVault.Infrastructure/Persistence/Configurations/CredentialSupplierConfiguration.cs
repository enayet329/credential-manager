using CredVault.Domain.Suppliers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CredVault.Infrastructure.Persistence.Configurations;

internal sealed class CredentialSupplierConfiguration : IEntityTypeConfiguration<CredentialSupplier>
{
    public void Configure(EntityTypeBuilder<CredentialSupplier> builder)
    {
        builder.ToTable("CredentialSuppliers");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasDefaultValueSql("NEWSEQUENTIALID()").ValueGeneratedNever();

        builder.Property(s => s.OrganizationId);
        builder.Property(s => s.SupplierType).HasConversion<int>();
        builder.Property(s => s.DisplayName).HasMaxLength(100).IsRequired();
        builder.Property(s => s.IsActive);
        builder.Property(s => s.CreatedAtUtc).HasColumnType("datetime2(7)");

        builder.HasIndex(s => new { s.OrganizationId, s.SupplierType });

        builder.Ignore(s => s.DomainEvents);
    }
}
