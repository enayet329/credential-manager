namespace CredVault.Application.Abstractions.Cryptography;

/// <summary>
/// Output of <see cref="IEnvelopeEncryptionService.EncryptAsync"/>. Carries the AES-GCM ciphertext, the
/// DEK wrapped by the current KEK, the data-encryption nonce + tag, and the KEK version used.
/// </summary>
/// <param name="Ciphertext">AES-256-GCM encrypted plaintext.</param>
/// <param name="WrappedDataKey">DEK encrypted by the KEK; layout is <c>nonce(12) || ciphertext(32) || tag(16)</c>.</param>
/// <param name="Nonce">12-byte GCM nonce used to encrypt <paramref name="Ciphertext"/>.</param>
/// <param name="AuthTag">16-byte GCM auth tag over <paramref name="Ciphertext"/> + AAD.</param>
/// <param name="KekVersion">Version of the KEK used to wrap <paramref name="WrappedDataKey"/>.</param>
public sealed record EncryptedPayload(
    byte[] Ciphertext,
    byte[] WrappedDataKey,
    byte[] Nonce,
    byte[] AuthTag,
    int KekVersion);
