using CredVault.Domain.Webhooks;
using CredVault.Infrastructure.Persistence.Conversions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CredVault.Infrastructure.Persistence.Configurations;

internal sealed class WebhookConfiguration : IEntityTypeConfiguration<Webhook>
{
    public void Configure(EntityTypeBuilder<Webhook> builder)
    {
        builder.ToTable("Webhooks");
        builder.HasKey(w => w.Id);
        builder.Property(w => w.Id).HasDefaultValueSql("NEWSEQUENTIALID()").ValueGeneratedNever();

        builder.Property(w => w.OrganizationId);
        builder.Property(w => w.Url).HasMaxLength(2048).IsRequired();
        builder.Property(w => w.SigningSecretReferenceId);
        builder.Property(w => w.IsActive);
        builder.Property(w => w.CreatedAtUtc).HasColumnType("datetime2(7)");

        // Events is exposed as IReadOnlyList<string>, backed by `_events`. Stored as a JSON array.
        var eventsProperty = builder.Property<List<string>>("_events")
            .HasColumnName("Events")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasConversion(StringListJsonConverter.Converter)
            .IsRequired();
        eventsProperty.Metadata.SetValueComparer(StringListJsonConverter.Comparer);

        builder.Ignore(w => w.Events);

        builder.HasIndex(w => new { w.OrganizationId, w.IsActive });

        builder.Property<byte[]>("RowVersion").IsRowVersion();

        builder.Ignore(w => w.DomainEvents);
    }
}
