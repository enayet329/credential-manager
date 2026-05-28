namespace CredVault.Domain.Suppliers;

/// <summary>
/// A labelled credential issuer scoped to an organisation. CredVault never calls a supplier's
/// external API — this is purely metadata used to pick a schema and group credentials in the UI.
/// </summary>
public sealed class CredentialSupplier : Entity
{
    /// <summary>FK to the owning organisation.</summary>
    public Guid OrganizationId { get; private init; }

    /// <summary>Catalogue value identifying the kind of supplier (drives schema selection).</summary>
    public SupplierType SupplierType { get; private init; }

    /// <summary>Human-readable label, e.g. <c>"OpenAI — Marketing team"</c>.</summary>
    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>Whether credentials may be created against this supplier.</summary>
    public bool IsActive { get; private set; }

    /// <summary>UTC instant the supplier was registered.</summary>
    public DateTime CreatedAtUtc { get; private init; }

    private CredentialSupplier() { }

    /// <summary>Creates a new active supplier in the given organisation.</summary>
    public static CredentialSupplier Create(
        Guid organizationId,
        SupplierType supplierType,
        string displayName,
        IDateTimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        if (organizationId == Guid.Empty)
            throw new DomainException("OrganizationId must not be empty.");
        if (string.IsNullOrWhiteSpace(displayName))
            throw new DomainException("Display name must not be empty.");
        if (displayName.Length > 100)
            throw new DomainException("Display name must be at most 100 characters.");

        return new CredentialSupplier
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            SupplierType = supplierType,
            DisplayName = displayName.Trim(),
            IsActive = true,
            CreatedAtUtc = clock.UtcNow,
        };
    }

    /// <summary>Updates the human-readable label. Supplier type is immutable.</summary>
    public void Rename(string newDisplayName)
    {
        if (string.IsNullOrWhiteSpace(newDisplayName))
            throw new DomainException("Display name must not be empty.");
        if (newDisplayName.Length > 100)
            throw new DomainException("Display name must be at most 100 characters.");
        DisplayName = newDisplayName.Trim();
    }

    /// <summary>Marks the supplier inactive. New credentials cannot be created against an inactive supplier.</summary>
    public void Deactivate() => IsActive = false;

    /// <summary>Re-activates the supplier.</summary>
    public void Activate() => IsActive = true;
}
