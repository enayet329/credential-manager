namespace CredVault.Domain.Credentials;

/// <summary>
/// Immutable description of the fields a particular <see cref="SupplierType"/> stores. Versioned so
/// existing credentials remain decryptable when the schema evolves.
/// </summary>
public sealed class CredentialSchema
{
    /// <summary>The supplier this schema applies to.</summary>
    public SupplierType SupplierType { get; }

    /// <summary>Schema version. Bump when fields are added/removed/renamed.</summary>
    public int Version { get; }

    /// <summary>Field definitions in display order.</summary>
    public IReadOnlyList<CredentialField> Fields { get; }

    /// <summary>Constructs a schema. Throws <see cref="DomainException"/> if fields are empty or contain duplicate keys.</summary>
    public CredentialSchema(SupplierType supplierType, int version, IReadOnlyList<CredentialField> fields)
    {
        ArgumentNullException.ThrowIfNull(fields);
        if (version <= 0)
            throw new DomainException("Schema version must be positive.");
        if (fields.Count == 0)
            throw new DomainException("Schema must declare at least one field.");

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var field in fields)
        {
            if (!seen.Add(field.Key))
                throw new DomainException($"Duplicate field key '{field.Key}'.");
        }

        SupplierType = supplierType;
        Version = version;
        Fields = fields;
    }
}
