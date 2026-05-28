using CredVault.Domain.Credentials;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CredVault.Api.Seeding;

/// <summary>
/// Forces the <see cref="ICredentialSchemaProvider"/> singleton to be instantiated at startup so that
/// the static seed schemas are ready before the first request. Phase 5 will extend this to load
/// organisation-defined schemas from the database.
/// </summary>
public sealed class CredentialSchemaSeeder : IHostedService
{
    private static readonly Action<ILogger, int, Exception?> LogSeeded =
        LoggerMessage.Define<int>(
            LogLevel.Information,
            new EventId(3001, nameof(CredentialSchemaSeeder)),
            "Seeded {Count} credential schemas at startup");

    private readonly ICredentialSchemaProvider _provider;
    private readonly ILogger<CredentialSchemaSeeder> _logger;

    public CredentialSchemaSeeder(ICredentialSchemaProvider provider, ILogger<CredentialSchemaSeeder> logger)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(logger);
        _provider = provider;
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Touch every supplier type so the registry initialisation cost is paid eagerly.
        var seeded = 0;
        foreach (var schema in CredentialSchemaRegistry.All)
        {
            _ = _provider.GetSchema(schema.SupplierType);
            seeded++;
        }
        LogSeeded(_logger, seeded, null);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
