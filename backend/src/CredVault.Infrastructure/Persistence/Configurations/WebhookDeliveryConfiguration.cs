using CredVault.Domain.Webhooks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CredVault.Infrastructure.Persistence.Configurations;

internal sealed class WebhookDeliveryConfiguration : IEntityTypeConfiguration<WebhookDelivery>
{
    public void Configure(EntityTypeBuilder<WebhookDelivery> builder)
    {
        builder.ToTable("WebhookDeliveries");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).HasDefaultValueSql("NEWSEQUENTIALID()").ValueGeneratedNever();

        builder.Property(d => d.WebhookId);
        builder.Property(d => d.EventType).HasMaxLength(100).IsRequired();
        builder.Property(d => d.PayloadJson).IsRequired();
        builder.Property(d => d.AttemptCount);
        builder.Property(d => d.NextAttemptAtUtc).HasColumnType("datetime2(7)");
        builder.Property(d => d.SucceededAtUtc).HasColumnType("datetime2(7)");
        builder.Property(d => d.LastResponseStatus);
        builder.Property(d => d.LastError).HasMaxLength(2000);

        builder.HasIndex(d => d.NextAttemptAtUtc)
            .HasFilter("[SucceededAtUtc] IS NULL")
            .HasDatabaseName("IX_WebhookDeliveries_DuePending");

        builder.Ignore(d => d.DomainEvents);
    }
}
