using CredVault.Infrastructure.Caching;
using CredVault.Infrastructure.Cryptography;
using CredVault.Infrastructure.Persistence;
using CredVault.Infrastructure.Persistence.Repositories;
using CredVault.Infrastructure.RateLimiting;
using CredVault.Infrastructure.Schemas;
using CredVault.Infrastructure.Vault;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CredVault.Infrastructure;

/// <summary>Wires the Infrastructure layer into the host's DI container.</summary>
public static class DependencyInjection
{
    /// <summary>Registers EF, encryption, caching, rate limiting, schemas, and repositories.</summary>
    public static IServiceCollection AddCredVaultInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Options
        services.AddOptions<MasterKeyOptions>()
            .Bind(configuration.GetSection(MasterKeyOptions.SectionName));
        services.AddOptions<CredVaultDbContextOptions>()
            .Bind(configuration.GetSection(CredVaultDbContextOptions.SectionName));
        services.AddOptions<PlaintextCacheOptions>()
            .Bind(configuration.GetSection(PlaintextCacheOptions.SectionName));
        services.AddOptions<VaultRateLimitOptions>()
            .Bind(configuration.GetSection(VaultRateLimitOptions.SectionName));

        // DbContext — resolve the connection string lazily so tests that override configuration after
        // service registration (WebApplicationFactory does this) still wire up correctly.
        services.AddDbContext<CredVaultDbContext>((sp, options) =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var connectionString = config.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is missing.");
            options.UseSqlServer(connectionString, sql => sql.MigrationsAssembly(typeof(CredVaultDbContext).Assembly.FullName));
        });

        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ICredentialRepository, CredentialRepository>();
        services.AddScoped<IServiceTokenRepository, ServiceTokenRepository>();
        services.AddScoped<ICredentialAccessLogRepository, CredentialAccessLogRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IWebhookDeliveryRepository, WebhookDeliveryRepository>();

        // Cryptography
        services.AddSingleton<IMasterKeyProvider, ConfigurationMasterKeyProvider>();
        services.AddSingleton<IEnvelopeEncryptionService, EnvelopeEncryptionService>();

        // Schemas
        services.AddSingleton<ICredentialSchemaProvider, CredentialSchemaProvider>();

        // Vault
        services.AddSingleton<PlaintextCache>();
        services.AddSingleton<IInProcessRateLimiter, InProcessRateLimiter>();
        services.AddScoped<ICredentialVaultService, CredentialVaultService>();

        // HybridCache (L1 in-memory; L2 SQL Server when toggled on)
        services.AddHybridCache(options =>
        {
            options.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(5),
                LocalCacheExpiration = TimeSpan.FromMinutes(1),
            };
        });

        if (configuration.GetValue<bool>("HybridCache:UseSqlL2"))
        {
            services.AddDistributedSqlServerCache(o =>
            {
                // Resolved at service-build time, after WebApplicationFactory overrides have applied.
                o.ConnectionString = configuration.GetConnectionString("DefaultConnection")!;
                o.SchemaName = "dbo";
                o.TableName = "DistributedCache";
            });
        }

        services.AddSingleton<IVaultCache, HybridVaultCache>();

        return services;
    }
}
