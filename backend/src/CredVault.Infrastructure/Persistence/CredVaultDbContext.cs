using CredVault.Domain.Audit;
using CredVault.Domain.Credentials;
using CredVault.Domain.Organizations;
using CredVault.Domain.Projects;
using CredVault.Domain.ServiceTokens;
using CredVault.Domain.Suppliers;
using CredVault.Domain.Users;
using CredVault.Domain.Webhooks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Environment = CredVault.Domain.Projects.Environment;

namespace CredVault.Infrastructure.Persistence;

/// <summary>
/// EF Core context for CredVault. Configures all aggregates via <c>IEntityTypeConfiguration&lt;T&gt;</c>
/// implementations in the <c>Configurations</c> folder. Append-only tables are enforced by the
/// repository layer — there are no <c>Update</c> or <c>Remove</c> operations on access logs, audit
/// logs, or rotations.
/// </summary>
public sealed class CredVaultDbContext : DbContext
{
    private readonly CredVaultDbContextOptions _credVaultOptions;

    /// <summary>Constructs the context with EF options and the JSON-column-type toggle.</summary>
    public CredVaultDbContext(
        DbContextOptions<CredVaultDbContext> options,
        IOptions<CredVaultDbContextOptions> credVaultOptions) : base(options)
    {
        ArgumentNullException.ThrowIfNull(credVaultOptions);
        _credVaultOptions = credVaultOptions.Value;
    }

    /// <summary>Organisations (aggregate root).</summary>
    public DbSet<Organization> Organizations => Set<Organization>();

    /// <summary>Org membership join rows.</summary>
    public DbSet<OrganizationMembership> OrganizationMemberships => Set<OrganizationMembership>();

    /// <summary>User accounts.</summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>One-use password-reset tokens.</summary>
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    /// <summary>Projects.</summary>
    public DbSet<Project> Projects => Set<Project>();

    /// <summary>Environments.</summary>
    public DbSet<Environment> Environments => Set<Environment>();

    /// <summary>Credential suppliers.</summary>
    public DbSet<CredentialSupplier> CredentialSuppliers => Set<CredentialSupplier>();

    /// <summary>Credentials (the central entity).</summary>
    public DbSet<Credential> Credentials => Set<Credential>();

    /// <summary>Append-only rotation history.</summary>
    public DbSet<CredentialRotation> CredentialRotations => Set<CredentialRotation>();

    /// <summary>Append-only per-credential access log.</summary>
    public DbSet<CredentialAccessLog> CredentialAccessLogs => Set<CredentialAccessLog>();

    /// <summary>Encrypted notes attached to credentials.</summary>
    public DbSet<CredentialNote> CredentialNotes => Set<CredentialNote>();

    /// <summary>Service tokens (CI/CD bearer credentials).</summary>
    public DbSet<ServiceToken> ServiceTokens => Set<ServiceToken>();

    /// <summary>Append-only org-level audit log.</summary>
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    /// <summary>Registered outbound webhooks.</summary>
    public DbSet<Webhook> Webhooks => Set<Webhook>();

    /// <summary>Per-attempt webhook delivery rows.</summary>
    public DbSet<WebhookDelivery> WebhookDeliveries => Set<WebhookDelivery>();

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CredVaultDbContext).Assembly);

        ApplyJsonColumnType(modelBuilder);
    }

    /// <summary>
    /// Switches columns that hold JSON-encoded strings between native <c>json</c> (SQL 2025+) and
    /// <c>nvarchar(max)</c> (SQL 2022 and earlier) at model-build time.
    /// </summary>
    private void ApplyJsonColumnType(ModelBuilder modelBuilder)
    {
        var jsonType = _credVaultOptions.JsonColumnType;

        modelBuilder.Entity<AuditLog>()
            .Property(l => l.MetadataJson).HasColumnType(jsonType);

        modelBuilder.Entity<ServiceToken>()
            .Property(t => t.ScopesJson).HasColumnType(jsonType);
        modelBuilder.Entity<ServiceToken>()
            .Property(t => t.IpAllowlistJson).HasColumnType(jsonType);

        modelBuilder.Entity<Webhook>()
            .Property("_events").HasColumnType(jsonType);

        modelBuilder.Entity<WebhookDelivery>()
            .Property(d => d.PayloadJson).HasColumnType(jsonType);
    }
}
