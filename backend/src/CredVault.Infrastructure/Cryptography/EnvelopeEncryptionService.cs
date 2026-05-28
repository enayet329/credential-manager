using System.Security.Cryptography;
using System.Text;

namespace CredVault.Infrastructure.Cryptography;

/// <summary>
/// AES-256-GCM envelope encryption. A fresh 32-byte data-encryption key (DEK) is generated per call,
/// the data is encrypted with the DEK bound to the supplied AAD (<c>encryptionContext</c>), then the
/// DEK itself is wrapped with the current master key-encryption key (KEK). Plaintext DEKs are zeroed
/// before the method returns.
/// </summary>
public sealed class EnvelopeEncryptionService : IEnvelopeEncryptionService
{
    private const int DataKeyLength = 32;
    private const int NonceLength = 12;
    private const int TagLength = 16;

    private readonly IMasterKeyProvider _kekProvider;

    /// <summary>Constructs the service with the given KEK provider.</summary>
    public EnvelopeEncryptionService(IMasterKeyProvider kekProvider)
    {
        ArgumentNullException.ThrowIfNull(kekProvider);
        _kekProvider = kekProvider;
    }

    /// <inheritdoc/>
    public Task<EncryptedPayload> EncryptAsync(byte[] plaintext, string encryptionContext, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(plaintext);
        ArgumentNullException.ThrowIfNull(encryptionContext);
        cancellationToken.ThrowIfCancellationRequested();

        var aad = Encoding.UTF8.GetBytes(encryptionContext);
        var kekVersion = _kekProvider.CurrentVersion;
        var kek = _kekProvider.GetKey(kekVersion);

        byte[]? dek = null;
        try
        {
            dek = RandomNumberGenerator.GetBytes(DataKeyLength);
            var dataNonce = RandomNumberGenerator.GetBytes(NonceLength);
            var ciphertext = new byte[plaintext.Length];
            var dataTag = new byte[TagLength];

            using (var dataCipher = new AesGcm(dek, TagLength))
                dataCipher.Encrypt(dataNonce, plaintext, ciphertext, dataTag, aad);

            var kekNonce = RandomNumberGenerator.GetBytes(NonceLength);
            var wrappedDek = new byte[DataKeyLength];
            var kekTag = new byte[TagLength];

            using (var kekCipher = new AesGcm(kek, TagLength))
                kekCipher.Encrypt(kekNonce, dek, wrappedDek, kekTag);

            // Layout: nonce(12) || wrappedDek(32) || tag(16) = 60 bytes
            var bundle = new byte[NonceLength + DataKeyLength + TagLength];
            Buffer.BlockCopy(kekNonce, 0, bundle, 0, NonceLength);
            Buffer.BlockCopy(wrappedDek, 0, bundle, NonceLength, DataKeyLength);
            Buffer.BlockCopy(kekTag, 0, bundle, NonceLength + DataKeyLength, TagLength);

            return Task.FromResult(new EncryptedPayload(ciphertext, bundle, dataNonce, dataTag, kekVersion));
        }
        finally
        {
            if (dek is not null) CryptographicOperations.ZeroMemory(dek);
        }
    }

    /// <inheritdoc/>
    public Task<byte[]> DecryptAsync(EncryptedPayload payload, string encryptionContext, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentNullException.ThrowIfNull(encryptionContext);
        cancellationToken.ThrowIfCancellationRequested();

        if (payload.WrappedDataKey.Length != NonceLength + DataKeyLength + TagLength)
            throw new CryptographicException("Wrapped data key has an unexpected length.");
        if (payload.Nonce.Length != NonceLength)
            throw new CryptographicException("Data nonce has an unexpected length.");
        if (payload.AuthTag.Length != TagLength)
            throw new CryptographicException("Auth tag has an unexpected length.");

        var kek = _kekProvider.GetKey(payload.KekVersion);
        var aad = Encoding.UTF8.GetBytes(encryptionContext);

        var kekNonce = new byte[NonceLength];
        var wrappedDek = new byte[DataKeyLength];
        var kekTag = new byte[TagLength];
        Buffer.BlockCopy(payload.WrappedDataKey, 0, kekNonce, 0, NonceLength);
        Buffer.BlockCopy(payload.WrappedDataKey, NonceLength, wrappedDek, 0, DataKeyLength);
        Buffer.BlockCopy(payload.WrappedDataKey, NonceLength + DataKeyLength, kekTag, 0, TagLength);

        var dek = new byte[DataKeyLength];
        try
        {
            using (var kekCipher = new AesGcm(kek, TagLength))
                kekCipher.Decrypt(kekNonce, wrappedDek, kekTag, dek);

            var plaintext = new byte[payload.Ciphertext.Length];
            using (var dataCipher = new AesGcm(dek, TagLength))
                dataCipher.Decrypt(payload.Nonce, payload.Ciphertext, payload.AuthTag, plaintext, aad);

            return Task.FromResult(plaintext);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(dek);
        }
    }
}
