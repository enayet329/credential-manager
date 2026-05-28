namespace CredVault.Domain.Credentials;

/// <summary>
/// Immutable bundle of the encrypted-secret material produced by the encryption service.
/// </summary>
/// <param name="EncryptedPayload">AES-256-GCM ciphertext of the credential JSON.</param>
/// <param name="WrappedDataKey">The data-encryption key wrapped by the current key-encryption key.</param>
/// <param name="Nonce">The 12-byte GCM nonce.</param>
/// <param name="AuthTag">The 16-byte GCM authentication tag.</param>
/// <param name="KekVersion">The KEK version used to wrap <paramref name="WrappedDataKey"/>.</param>
/// <param name="MaskedPreview">≤16-character preview shown in listings (e.g. <c>"sk-***xyz"</c>).</param>
public sealed record CredentialEnvelope(
    byte[] EncryptedPayload,
    byte[] WrappedDataKey,
    byte[] Nonce,
    byte[] AuthTag,
    int KekVersion,
    string MaskedPreview)
{
    /// <summary>Required nonce length in bytes for AES-GCM.</summary>
    public const int NonceLength = 12;

    /// <summary>Required auth-tag length in bytes for AES-GCM.</summary>
    public const int AuthTagLength = 16;

    /// <summary>Maximum length of <see cref="MaskedPreview"/>.</summary>
    public const int MaxPreviewLength = 16;

    /// <summary>Validates envelope shape. Throws <see cref="DomainException"/> on any constraint violation.</summary>
    public void Validate()
    {
        ArgumentNullException.ThrowIfNull(EncryptedPayload);
        ArgumentNullException.ThrowIfNull(WrappedDataKey);
        ArgumentNullException.ThrowIfNull(Nonce);
        ArgumentNullException.ThrowIfNull(AuthTag);
        ArgumentNullException.ThrowIfNull(MaskedPreview);

        if (EncryptedPayload.Length == 0)
            throw new DomainException("EncryptedPayload must not be empty.");
        if (WrappedDataKey.Length == 0)
            throw new DomainException("WrappedDataKey must not be empty.");
        if (Nonce.Length != NonceLength)
            throw new DomainException($"Nonce must be exactly {NonceLength} bytes.");
        if (AuthTag.Length != AuthTagLength)
            throw new DomainException($"AuthTag must be exactly {AuthTagLength} bytes.");
        if (KekVersion <= 0)
            throw new DomainException("KekVersion must be positive.");
        if (MaskedPreview.Length > MaxPreviewLength)
            throw new DomainException($"MaskedPreview must be at most {MaxPreviewLength} characters.");
    }
}
