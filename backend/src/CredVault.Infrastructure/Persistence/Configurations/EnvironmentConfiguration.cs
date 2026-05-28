using CredVault.Infrastructure.Persistence.Conversions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Environment = CredVault.Domain.Projects.Environment;

namespace CredVault.Infrastructure.Persistence.Configurations;

internal sealed class EnvironmentConfiguration : IEntityTypeConfiguration<Environment>
{
    public void Configure(EntityTypeBuilder<Environment> builder)
    {
        builder.ToTable("Environments");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasDefaultValueSql("NEWSEQUENTIALID()").ValueGeneratedNever();

        builder.Property(e => e.ProjectId);
        builder.Property(e => e.Name).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Slug)
            .HasConversion(ValueObjectConverters.Slug)
            .HasMaxLength(Slug.MaxLength)
            .IsRequired();
        builder.Property(e => e.Type).HasConversion<int>();
        builder.Property(e => e.CreatedAtUtc).HasColumnType("datetime2(7)");

        builder.HasIndex(e => new { e.ProjectId, e.Slug }).IsUnique();

        builder.Property<byte[]>("RowVersion").IsRowVersion();

        builder.Ignore(e => e.DomainEvents);
    }
}
