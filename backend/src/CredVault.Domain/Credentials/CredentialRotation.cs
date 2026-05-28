namespace CredVault.Domain.Credentials;

/// <summary>
/// Append-only history of credential rotations. Captures the encrypted material that was in place
/// <em>before</em> the rotation so SOC 2 audits can prove the secret existed at a point in time —
/// the API exposes only <c>"rotated on DATE by USER"</c>, never decrypts these rows.
/// </summary>
public sealed class CredentialRotation : Entity
{
    /// <summary>FK to the credential this row belongs to.</summary>
    public Guid CredentialId { get; private init; }

    /// <summary>UTC instant the rotation occurred.</summary>
    public DateTime RotatedAtUtc { get; private init; }

    /// <summary>User who triggered the rotation.</summary>
    public Guid RotatedByUserId { get; private init; }

    /// <summary>The previous AES-GCM ciphertext.</summary>
    public byte[] PreviousEncryptedPayload { get; private init; } = [];

    /// <summary>The previous wrapped data key.</summary>
    public byte[] PreviousWrappedDataKey { get; private init; } = [];

    /// <summary>The previous AES-GCM nonce.</summary>
    public byte[] PreviousNonce { get; private init; } = [];

    /// <summary>The previous AES-GCM authentication tag.</summary>
    public byte[] PreviousAuthTag { get; private init; } = [];

    /// <summary>The KEK version under which the previous data key was wrapped.</summary>
    public int PreviousKekVersion { get; private init; }

    /// <summary>Optional human reason for the rotation.</summary>
    public string? Reason { get; private init; }

    private CredentialRotation() { }

    internal static CredentialRotation Create(
        Guid credentialId,
        Guid rotatedByUserId,
        CredentialEnvelope previous,
        string? reason,
        DateTime rotatedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(previous);
        if (rotatedByUserId == Guid.Empty)
            throw new DomainException("RotatedByUserId must not be empty.");
        previous.Validate();

        return new CredentialRotation
        {
            Id = Guid.NewGuid(),
            CredentialId = credentialId,
            RotatedAtUtc = rotatedAtUtc,
            RotatedByUserId = rotatedByUserId,
            PreviousEncryptedPayload = previous.EncryptedPayload,
            PreviousWrappedDataKey = previous.WrappedDataKey,
            PreviousNonce = previous.Nonce,
            PreviousAuthTag = previous.AuthTag,
            PreviousKekVersion = previous.KekVersion,
            Reason = reason,
        };
    }
}
