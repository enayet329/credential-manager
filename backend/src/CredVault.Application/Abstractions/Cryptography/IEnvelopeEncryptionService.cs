namespace CredVault.Application.Abstractions.Cryptography;

/// <summary>
/// AES-256-GCM envelope encryption with per-call data-encryption keys and AAD-bound encryption
/// contexts. Decryption requires the exact <c>encryptionContext</c> used at encrypt time.
/// </summary>
public interface IEnvelopeEncryptionService
{
    /// <summary>Encrypts <paramref name="plaintext"/> binding the ciphertext to <paramref name="encryptionContext"/> via AES-GCM AAD.</summary>
    Task<EncryptedPayload> EncryptAsync(byte[] plaintext, string encryptionContext, CancellationToken cancellationToken);

    /// <summary>Decrypts a previously-produced envelope. Throws <see cref="System.Security.Cryptography.CryptographicException"/> on AAD mismatch or tampering.</summary>
    Task<byte[]> DecryptAsync(EncryptedPayload payload, string encryptionContext, CancellationToken cancellationToken);
}
