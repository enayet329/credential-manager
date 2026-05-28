using System.Collections.Concurrent;
using CredVault.Domain.Credentials;

namespace CredVault.Infrastructure.Schemas;

/// <summary>
/// Phase-3 schema provider. Seeds from the static <see cref="CredentialSchemaRegistry"/> and allows
/// runtime registration. Phase 4 will swap this for a DB-backed implementation.
/// </summary>
public sealed class CredentialSchemaProvider : ICredentialSchemaProvider
{
    // SupplierType -> Version -> Schema
    private readonly ConcurrentDictionary<SupplierType, ConcurrentDictionary<int, CredentialSchema>> _schemas;

    /// <summary>Seeds the provider from the built-in registry.</summary>
    public CredentialSchemaProvider()
    {
        _schemas = new ConcurrentDictionary<SupplierType, ConcurrentDictionary<int, CredentialSchema>>();
        foreach (var schema in CredentialSchemaRegistry.All)
        {
            var versions = _schemas.GetOrAdd(schema.SupplierType, _ => new ConcurrentDictionary<int, CredentialSchema>());
            versions[schema.Version] = schema;
        }
    }

    /// <inheritdoc/>
    public CredentialSchema GetSchema(SupplierType supplierType, int? version = null)
    {
        if (!_schemas.TryGetValue(supplierType, out var versions))
            throw new DomainException($"No schema registered for supplier '{supplierType}'.");

        if (version is { } requested)
        {
            return versions.TryGetValue(requested, out var schema)
                ? schema
                : throw new DomainException($"No schema v{requested} registered for supplier '{supplierType}'.");
        }

        return versions.Values.OrderByDescending(s => s.Version).First();
    }

    /// <inheritdoc/>
    public Task RegisterSchemaAsync(CredentialSchema schema, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(schema);
        var versions = _schemas.GetOrAdd(schema.SupplierType, _ => new ConcurrentDictionary<int, CredentialSchema>());

        var maxExisting = versions.IsEmpty ? 0 : versions.Keys.Max();
        if (schema.Version <= maxExisting)
            throw new DomainException(
                $"Schema v{schema.Version} for supplier '{schema.SupplierType}' must be strictly greater than the latest registered version v{maxExisting}.");

        versions[schema.Version] = schema;
        return Task.CompletedTask;
    }
}
