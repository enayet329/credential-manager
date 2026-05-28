using CredVault.Domain.Credentials;

namespace CredVault.Application.Abstractions.Schemas;

/// <summary>
/// Lookup for credential schemas. Phase 3 uses the static <see cref="CredentialSchemaRegistry"/>;
/// Phase 4 will plug in a DB-backed implementation that supports organisation-defined schemas.
/// </summary>
public interface ICredentialSchemaProvider
{
    /// <summary>Returns the schema for <paramref name="supplierType"/>. <paramref name="version"/> = null selects the latest.</summary>
    CredentialSchema GetSchema(SupplierType supplierType, int? version = null);

    /// <summary>Registers a new schema version at runtime. Throws if the version is not strictly greater than existing ones.</summary>
    Task RegisterSchemaAsync(CredentialSchema schema, CancellationToken cancellationToken = default);
}
