using CredVault.Domain.Credentials.Events;

namespace CredVault.Domain.Credentials;

/// <summary>
/// The central entity of CredVault. Holds the encrypted secret material plus identifying metadata
/// (which supplier, which environment, which slug). Decryption itself lives in the application layer
/// — the domain never sees plaintext.
/// </summary>
public sealed class Credential : Entity
{
    private readonly List<CredentialRotation> _rotations = [];

    /// <summary>FK to the supplier that issued this credential.</summary>
    public Guid SupplierId { get; private init; }

    /// <summary>FK to the environment this credential is scoped to.</summary>
    public Guid EnvironmentId { get; private init; }

    /// <summary>Human-readable label.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>URL-safe slug, unique within (EnvironmentId, SupplierId).</summary>
    public Slug Slug { get; private set; } = null!;

    /// <summary>UTC instant the credential was created.</summary>
    public DateTime CreatedAtUtc { get; private init; }

    /// <summary>UTC instant of the most recent rotation. Equal to <see cref="CreatedAtUtc"/> until first rotation.</summary>
    public DateTime RotatedAtUtc { get; private set; }

    /// <summary>Optional expiry timestamp. When set, must be in the future at create/rotate time.</summary>
    public DateTime? ExpiresAtUtc { get; private set; }

    /// <summary>AES-256-GCM ciphertext of the credential JSON.</summary>
    public byte[] EncryptedPayload { get; private set; } = [];

    /// <summary>Data-encryption key wrapped by the current KEK.</summary>
    public byte[] WrappedDataKey { get; private set; } = [];

    /// <summary>AES-GCM nonce (12 bytes).</summary>
    public byte[] Nonce { get; private set; } = [];

    /// <summary>AES-GCM authentication tag (16 bytes).</summary>
    public byte[] AuthTag { get; private set; } = [];

    /// <summary>KEK version used to wrap the current data key.</summary>
    public int KekVersion { get; private set; }

    /// <summary>Version of the supplier's <see cref="CredentialSchema"/> that the JSON conforms to.</summary>
    public int CredentialSchemaVersion { get; private init; }

    /// <summary>UTC timestamp of the most recent successful decrypt.</summary>
    public DateTime? LastAccessedAtUtc { get; private set; }

    /// <summary>Total number of successful decrypts.</summary>
    public long AccessCount { get; private set; }

    /// <summary>≤16-char display preview computed at encrypt time. Safe to show in listings.</summary>
    public string MaskedPreview { get; private set; } = string.Empty;

    /// <summary>Whether the credential has been administratively revoked.</summary>
    public bool IsRevoked { get; private set; }

    /// <summary>UTC instant of revocation. <c>null</c> while the credential is active.</summary>
    public DateTime? RevokedAtUtc { get; private set; }

    /// <summary>User who revoked the credential. <c>null</c> while the credential is active.</summary>
    public Guid? RevokedByUserId { get; private set; }

    /// <summary>Append-only rotation history. Each entry holds the pre-rotation encrypted material.</summary>
    public IReadOnlyList<CredentialRotation> Rotations => _rotations;

    private Credential() { }

    /// <summary>Creates a new credential with the given envelope and metadata. Emits <see cref="CredentialCreated"/>.</summary>
    public static Credential Create(
        Guid supplierId,
        Guid environmentId,
        string name,
        Slug slug,
        CredentialEnvelope envelope,
        int schemaVersion,
        DateTime? expiresAtUtc,
        IDateTimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(slug);
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentNullException.ThrowIfNull(clock);
        if (supplierId == Guid.Empty)
            throw new DomainException("SupplierId must not be empty.");
        if (environmentId == Guid.Empty)
            throw new DomainException("EnvironmentId must not be empty.");
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Credential name must not be empty.");
        if (name.Length > 100)
            throw new DomainException("Credential name must be at most 100 characters.");
        if (schemaVersion <= 0)
            throw new DomainException("Schema version must be positive.");
        envelope.Validate();

        var now = clock.UtcNow;
        if (expiresAtUtc is { } exp && exp <= now)
            throw new DomainException("ExpiresAtUtc must be in the future.");

        var credential = new Credential
        {
            Id = Guid.NewGuid(),
            SupplierId = supplierId,
            EnvironmentId = environmentId,
            Name = name.Trim(),
            Slug = slug,
            CreatedAtUtc = now,
            RotatedAtUtc = now,
            ExpiresAtUtc = expiresAtUtc,
            EncryptedPayload = envelope.EncryptedPayload,
            WrappedDataKey = envelope.WrappedDataKey,
            Nonce = envelope.Nonce,
            AuthTag = envelope.AuthTag,
            KekVersion = envelope.KekVersion,
            CredentialSchemaVersion = schemaVersion,
            MaskedPreview = envelope.MaskedPreview,
            AccessCount = 0,
            IsRevoked = false,
        };
        credential.Raise(new CredentialCreated(credential.Id, supplierId, environmentId, now));
        return credential;
    }

    /// <summary>
    /// Replaces the encrypted material. The previous material is appended to <see cref="Rotations"/>.
    /// Emits <see cref="CredentialRotated"/>.
    /// </summary>
    public void Rotate(
        CredentialEnvelope newEnvelope,
        Guid rotatedByUserId,
        string? reason,
        DateTime? newExpiresAtUtc,
        IDateTimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(newEnvelope);
        ArgumentNullException.ThrowIfNull(clock);
        if (rotatedByUserId == Guid.Empty)
            throw new DomainException("RotatedByUserId must not be empty.");
        if (IsRevoked)
            throw new DomainException("Cannot rotate a revoked credential.");
        newEnvelope.Validate();

        var now = clock.UtcNow;
        if (newExpiresAtUtc is { } exp && exp <= now)
            throw new DomainException("ExpiresAtUtc must be in the future.");

        var previous = new CredentialEnvelope(
            EncryptedPayload, WrappedDataKey, Nonce, AuthTag, KekVersion, MaskedPreview);

        _rotations.Add(CredentialRotation.Create(Id, rotatedByUserId, previous, reason, now));

        EncryptedPayload = newEnvelope.EncryptedPayload;
        WrappedDataKey = newEnvelope.WrappedDataKey;
        Nonce = newEnvelope.Nonce;
        AuthTag = newEnvelope.AuthTag;
        KekVersion = newEnvelope.KekVersion;
        MaskedPreview = newEnvelope.MaskedPreview;
        RotatedAtUtc = now;
        ExpiresAtUtc = newExpiresAtUtc;

        Raise(new CredentialRotated(Id, rotatedByUserId, now));
    }

    /// <summary>
    /// Records a successful decrypt. Updates <see cref="LastAccessedAtUtc"/> and <see cref="AccessCount"/>
    /// and emits <see cref="CredentialAccessed"/>. The dedicated <see cref="CredentialAccessLog"/> row is
    /// produced separately by the application layer.
    /// </summary>
    public void RecordAccess(
        ActorType actorType,
        Guid actorId,
        AccessMethod accessMethod,
        IDateTimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        if (actorId == Guid.Empty)
            throw new DomainException("ActorId must not be empty.");
        if (IsRevoked)
            throw new DomainException("Cannot access a revoked credential.");

        var now = clock.UtcNow;
        LastAccessedAtUtc = now;
        AccessCount++;
        Raise(new CredentialAccessed(Id, actorType, actorId, accessMethod, now));
    }

    /// <summary>Administratively revokes the credential. Idempotent. Emits <see cref="CredentialRevoked"/>.</summary>
    public void Revoke(Guid revokedByUserId, IDateTimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        if (revokedByUserId == Guid.Empty)
            throw new DomainException("RevokedByUserId must not be empty.");
        if (IsRevoked)
            throw new DomainException("Credential is already revoked.");

        var now = clock.UtcNow;
        IsRevoked = true;
        RevokedAtUtc = now;
        RevokedByUserId = revokedByUserId;
        Raise(new CredentialRevoked(Id, revokedByUserId, now));
    }

    /// <summary>Whether the credential is past its expiry instant at the given clock reading.</summary>
    public bool IsExpired(IDateTimeProvider clock) =>
        ExpiresAtUtc is { } exp && clock.UtcNow >= exp;
}
