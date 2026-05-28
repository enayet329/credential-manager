using CredVault.Domain.Organizations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CredVault.Infrastructure.Persistence.Configurations;

internal sealed class OrganizationMembershipConfiguration : IEntityTypeConfiguration<OrganizationMembership>
{
    public void Configure(EntityTypeBuilder<OrganizationMembership> builder)
    {
        builder.ToTable("OrganizationMemberships");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasDefaultValueSql("NEWSEQUENTIALID()").ValueGeneratedNever();

        builder.Property(m => m.OrganizationId);
        builder.Property(m => m.UserId);
        builder.Property(m => m.Role).HasConversion<int>();
        builder.Property(m => m.JoinedAtUtc).HasColumnType("datetime2(7)");

        builder.HasIndex(m => new { m.OrganizationId, m.UserId }).IsUnique();

        builder.Ignore(m => m.DomainEvents);
    }
}
