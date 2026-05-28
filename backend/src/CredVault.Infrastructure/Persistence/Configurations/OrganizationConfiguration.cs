using CredVault.Domain.Organizations;
using CredVault.Infrastructure.Persistence.Conversions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CredVault.Infrastructure.Persistence.Configurations;

internal sealed class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.ToTable("Organizations");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).HasDefaultValueSql("NEWSEQUENTIALID()").ValueGeneratedNever();

        builder.Property(o => o.Name).HasMaxLength(100).IsRequired();
        builder.Property(o => o.Slug)
            .HasConversion(ValueObjectConverters.Slug)
            .HasMaxLength(Slug.MaxLength)
            .IsRequired();
        builder.Property(o => o.CreatedAtUtc).HasColumnType("datetime2(7)");
        builder.Property(o => o.IsActive);

        builder.HasIndex(o => o.Slug).IsUnique();

        builder.Property<byte[]>("RowVersion").IsRowVersion();

        builder.HasMany(o => o.Memberships)
            .WithOne()
            .HasForeignKey(m => m.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(Organization.Memberships))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(o => o.DomainEvents);
    }
}
