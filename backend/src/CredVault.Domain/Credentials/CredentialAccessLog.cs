namespace CredVault.Domain.Credentials;

/// <summary>
/// Append-only access-log row. High-volume — keep narrow and index <c>(CredentialId, AccessedAtUtc DESC)</c>;
/// partition by month at Phase 7.
/// </summary>
public sealed class CredentialAccessLog : Entity
{
    /// <summary>FK to the credential that was accessed.</summary>
    public Guid CredentialId { get; private init; }

    /// <summary>UTC instant of the access attempt.</summary>
    public DateTime AccessedAtUtc { get; private init; }

    /// <summary>Whether a user or a service token initiated the access.</summary>
    public ActorType ActorType { get; private init; }

    /// <summary>Identity of the actor (user id or service-token id).</summary>
    public Guid ActorId { get; private init; }

    /// <summary>Source IP of the request. Empty string when not known (e.g. CLI offline).</summary>
    public string IpAddress { get; private init; } = string.Empty;

    /// <summary>User-agent string of the caller.</summary>
    public string UserAgent { get; private init; } = string.Empty;

    /// <summary>Transport used.</summary>
    public AccessMethod AccessMethod { get; private init; }

    /// <summary>Whether the access succeeded or was denied.</summary>
    public AccessOutcome Outcome { get; private init; }

    private CredentialAccessLog() { }

    /// <summary>Records a credential-access attempt. Always succeeds — there is no update or delete.</summary>
    public static CredentialAccessLog Record(
        Guid credentialId,
        ActorType actorType,
        Guid actorId,
        string ipAddress,
        string userAgent,
        AccessMethod accessMethod,
        AccessOutcome outcome,
        IDateTimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(ipAddress);
        ArgumentNullException.ThrowIfNull(userAgent);
        if (credentialId == Guid.Empty)
            throw new DomainException("CredentialId must not be empty.");
        if (actorId == Guid.Empty)
            throw new DomainException("ActorId must not be empty.");

        return new CredentialAccessLog
        {
            Id = Guid.NewGuid(),
            CredentialId = credentialId,
            AccessedAtUtc = clock.UtcNow,
            ActorType = actorType,
            ActorId = actorId,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            AccessMethod = accessMethod,
            Outcome = outcome,
        };
    }
}
