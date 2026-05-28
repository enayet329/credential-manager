using CredVault.Domain.ServiceTokens.Events;

namespace CredVault.Domain.ServiceTokens;

/// <summary>
/// A long-lived bearer credential used by CI/CD pipelines and the CLI. The plaintext token is shown
/// to the creator once; only its HMAC is stored. Scoped by (project, environment, permission) rules.
/// </summary>
public sealed class ServiceToken : Entity
{
    /// <summary>The canonical prefix for live tokens; embedded in the plaintext for visual identification.</summary>
    public const string LivePrefix = "svc_live_";

    /// <summary>FK to the owning organisation.</summary>
    public Guid OrganizationId { get; private init; }

    /// <summary>Optional project scope. <c>null</c> means the token is org-wide.</summary>
    public Guid? ProjectId { get; private init; }

    /// <summary>Public prefix portion of the token (used to look the row up by the first 12 chars).</summary>
    public string Prefix { get; private init; } = LivePrefix;

    /// <summary>HMAC of the full plaintext token using a server-side pepper.</summary>
    public byte[] HmacHash { get; private init; } = [];

    /// <summary>Human-readable label.</summary>
    public string Label { get; private set; } = string.Empty;

    /// <summary>UTC instant the token was created.</summary>
    public DateTime CreatedAtUtc { get; private init; }

    /// <summary>UTC instant the token was most recently presented at the auth layer.</summary>
    public DateTime? LastUsedAtUtc { get; private set; }

    /// <summary>Optional expiry timestamp.</summary>
    public DateTime? ExpiresAtUtc { get; private init; }

    /// <summary>UTC instant of revocation. <c>null</c> while the token is active.</summary>
    public DateTime? RevokedAtUtc { get; private set; }

    /// <summary>User who created the token.</summary>
    public Guid CreatedByUserId { get; private init; }

    /// <summary>Serialised list of <see cref="ServiceTokenScope"/> rules. Application layer is responsible for the JSON shape.</summary>
    public string ScopesJson { get; private init; } = "[]";

    /// <summary>Optional CIDR allow-list. v1.5 feature — the field exists but is not enforced yet.</summary>
    public string? IpAllowlistJson { get; private set; }

    private ServiceToken() { }

    /// <summary>Creates a new active service token. Emits <see cref="ServiceTokenCreated"/>.</summary>
    public static ServiceToken Create(
        Guid organizationId,
        Guid? projectId,
        byte[] hmacHash,
        string label,
        string scopesJson,
        Guid createdByUserId,
        DateTime? expiresAtUtc,
        IDateTimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(hmacHash);
        ArgumentNullException.ThrowIfNull(scopesJson);
        ArgumentNullException.ThrowIfNull(clock);
        if (organizationId == Guid.Empty)
            throw new DomainException("OrganizationId must not be empty.");
        if (createdByUserId == Guid.Empty)
            throw new DomainException("CreatedByUserId must not be empty.");
        if (hmacHash.Length == 0)
            throw new DomainException("HmacHash must not be empty.");
        if (string.IsNullOrWhiteSpace(label))
            throw new DomainException("Label must not be empty.");
        if (label.Length > 100)
            throw new DomainException("Label must be at most 100 characters.");

        var now = clock.UtcNow;
        if (expiresAtUtc is { } exp && exp <= now)
            throw new DomainException("ExpiresAtUtc must be in the future.");

        var token = new ServiceToken
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ProjectId = projectId,
            Prefix = LivePrefix,
            HmacHash = hmacHash,
            Label = label.Trim(),
            CreatedAtUtc = now,
            ExpiresAtUtc = expiresAtUtc,
            CreatedByUserId = createdByUserId,
            ScopesJson = scopesJson,
        };
        token.Raise(new ServiceTokenCreated(token.Id, organizationId, projectId, createdByUserId, now));
        return token;
    }

    /// <summary>Records a successful auth presentation. Raises <see cref="ServiceTokenFirstUsed"/> the very first time.</summary>
    public void RecordUsage(IDateTimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        if (RevokedAtUtc is not null)
            throw new DomainException("Cannot use a revoked service token.");
        if (ExpiresAtUtc is { } exp && clock.UtcNow >= exp)
            throw new DomainException("Cannot use an expired service token.");

        var now = clock.UtcNow;
        var firstUse = LastUsedAtUtc is null;
        LastUsedAtUtc = now;
        if (firstUse)
            Raise(new ServiceTokenFirstUsed(Id, now));
    }

    /// <summary>Marks the token revoked. Emits <see cref="ServiceTokenRevoked"/>. Idempotent revocation is rejected.</summary>
    public void Revoke(IDateTimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        if (RevokedAtUtc is not null)
            throw new DomainException("Service token is already revoked.");
        var now = clock.UtcNow;
        RevokedAtUtc = now;
        Raise(new ServiceTokenRevoked(Id, now));
    }

    /// <summary>Updates the human-readable label.</summary>
    public void Rename(string newLabel)
    {
        if (string.IsNullOrWhiteSpace(newLabel))
            throw new DomainException("Label must not be empty.");
        if (newLabel.Length > 100)
            throw new DomainException("Label must be at most 100 characters.");
        Label = newLabel.Trim();
    }

    /// <summary>Updates the IP allow-list JSON (not enforced until v1.5).</summary>
    public void UpdateIpAllowlist(string? ipAllowlistJson) => IpAllowlistJson = ipAllowlistJson;

    /// <summary>Whether the token is usable at the given clock reading.</summary>
    public bool IsActive(IDateTimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        if (RevokedAtUtc is not null)
            return false;
        if (ExpiresAtUtc is { } exp && clock.UtcNow >= exp)
            return false;
        return true;
    }
}
