namespace CredVault.Application.Abstractions.Vault;

/// <summary>
/// Dynamic-field credential vault. Validates inbound fields against the supplier's schema, encrypts
/// with envelope encryption, persists, and writes a per-call access log on retrieve.
/// </summary>
public interface ICredentialVaultService
{
    /// <summary>Encrypts and stores a new credential. Returns the credential's identity.</summary>
    Task<Guid> StoreAsync(
        Guid supplierId,
        Guid environmentId,
        string name,
        Slug slug,
        IReadOnlyDictionary<string, string> fields,
        DateTime? expiresAtUtc,
        CancellationToken cancellationToken);

    /// <summary>
    /// Decrypts a credential and writes a <c>CredentialAccessLog</c> row attributed to <paramref name="access"/>.
    /// Subject to per-credential rate limiting.
    /// </summary>
    Task<IReadOnlyDictionary<string, string>> RetrieveAsync(
        Guid credentialId,
        CredentialAccessContext access,
        CancellationToken cancellationToken);

    /// <summary>Rotates a credential. The previous encrypted material is moved to <c>CredentialRotation</c>.</summary>
    Task RotateAsync(
        Guid credentialId,
        IReadOnlyDictionary<string, string> newFields,
        DateTime? newExpiresAtUtc,
        string? reason,
        CancellationToken cancellationToken);
}
