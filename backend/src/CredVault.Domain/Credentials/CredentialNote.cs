namespace CredVault.Domain.Credentials;

/// <summary>
/// Runbook-style note attached to a credential. Encrypted with the same envelope service as the
/// credential itself; the encryption AAD (additional authenticated data) includes the credential id
/// so a note cannot be re-pointed at a different credential.
/// </summary>
public sealed class CredentialNote : Entity
{
    /// <summary>FK to the parent credential.</summary>
    public Guid CredentialId { get; private init; }

    /// <summary>UTC instant the note was created.</summary>
    public DateTime CreatedAtUtc { get; private init; }

    /// <summary>User who authored the note.</summary>
    public Guid CreatedByUserId { get; private init; }

    /// <summary>AES-GCM ciphertext of the note body.</summary>
    public byte[] EncryptedContent { get; private init; } = [];

    /// <summary>Data-encryption key wrapped by the current KEK.</summary>
    public byte[] WrappedDataKey { get; private init; } = [];

    /// <summary>AES-GCM nonce.</summary>
    public byte[] Nonce { get; private init; } = [];

    /// <summary>AES-GCM authentication tag.</summary>
    public byte[] AuthTag { get; private init; } = [];

    /// <summary>KEK version used to wrap the data key.</summary>
    public int KekVersion { get; private init; }

    private CredentialNote() { }

    /// <summary>Creates an encrypted note pointing at the given credential.</summary>
    public static CredentialNote Create(
        Guid credentialId,
        Guid createdByUserId,
        CredentialEnvelope envelope,
        IDateTimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentNullException.ThrowIfNull(clock);
        if (credentialId == Guid.Empty)
            throw new DomainException("CredentialId must not be empty.");
        if (createdByUserId == Guid.Empty)
            throw new DomainException("CreatedByUserId must not be empty.");
        envelope.Validate();

        return new CredentialNote
        {
            Id = Guid.NewGuid(),
            CredentialId = credentialId,
            CreatedAtUtc = clock.UtcNow,
            CreatedByUserId = createdByUserId,
            EncryptedContent = envelope.EncryptedPayload,
            WrappedDataKey = envelope.WrappedDataKey,
            Nonce = envelope.Nonce,
            AuthTag = envelope.AuthTag,
            KekVersion = envelope.KekVersion,
        };
    }
}
