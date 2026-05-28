namespace CredVault.Domain.Audit;

/// <summary>
/// Append-only record of an org-level event (member added, role changed, supplier created, etc.).
/// Per-credential reads go to <c>CredentialAccessLog</c> instead to keep this table small.
/// </summary>
public sealed class AuditLog : Entity
{
    /// <summary>FK to the organisation this event belongs to.</summary>
    public Guid OrganizationId { get; private init; }

    /// <summary>Acting user, if the event was triggered by a human. Mutually exclusive with <see cref="ActorServiceTokenId"/>.</summary>
    public Guid? ActorUserId { get; private init; }

    /// <summary>Acting service token, if the event was triggered programmatically.</summary>
    public Guid? ActorServiceTokenId { get; private init; }

    /// <summary>Caller IP at the time of the event. Empty string when not known.</summary>
    public string ActorIpAddress { get; private init; } = string.Empty;

    /// <summary>Caller user-agent at the time of the event.</summary>
    public string ActorUserAgent { get; private init; } = string.Empty;

    /// <summary>UTC instant of the event.</summary>
    public DateTime OccurredAtUtc { get; private init; }

    /// <summary>Verb describing what happened (e.g. <c>"member.added"</c>, <c>"supplier.created"</c>).</summary>
    public string Action { get; private init; } = string.Empty;

    /// <summary>Type of the target entity (e.g. <c>"Credential"</c>, <c>"ServiceToken"</c>).</summary>
    public string TargetType { get; private init; } = string.Empty;

    /// <summary>Identifier of the target entity. Stored as string to accommodate composite keys.</summary>
    public string TargetId { get; private init; } = string.Empty;

    /// <summary>Application-specific JSON metadata. The domain does not validate its shape.</summary>
    public string MetadataJson { get; private init; } = "{}";

    /// <summary>Whether the action succeeded or was denied.</summary>
    public AccessOutcome Outcome { get; private init; }

    /// <summary>Optional human-readable reason — typically populated on <see cref="AccessOutcome.Denied"/>.</summary>
    public string? Reason { get; private init; }

    private AuditLog() { }

    /// <summary>Writes a new audit row. Validates that exactly one actor (user or service token) is supplied.</summary>
    public static AuditLog Record(
        Guid organizationId,
        Guid? actorUserId,
        Guid? actorServiceTokenId,
        string actorIpAddress,
        string actorUserAgent,
        string action,
        string targetType,
        string targetId,
        string metadataJson,
        AccessOutcome outcome,
        string? reason,
        IDateTimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(actorIpAddress);
        ArgumentNullException.ThrowIfNull(actorUserAgent);
        ArgumentNullException.ThrowIfNull(metadataJson);
        ArgumentNullException.ThrowIfNull(clock);
        if (organizationId == Guid.Empty)
            throw new DomainException("OrganizationId must not be empty.");
        if (string.IsNullOrWhiteSpace(action))
            throw new DomainException("Action must not be empty.");
        if (string.IsNullOrWhiteSpace(targetType))
            throw new DomainException("TargetType must not be empty.");
        if (string.IsNullOrWhiteSpace(targetId))
            throw new DomainException("TargetId must not be empty.");

        var userPresent = actorUserId is { } u && u != Guid.Empty;
        var tokenPresent = actorServiceTokenId is { } s && s != Guid.Empty;
        if (userPresent == tokenPresent)
            throw new DomainException("Exactly one of ActorUserId or ActorServiceTokenId must be set.");

        return new AuditLog
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ActorUserId = userPresent ? actorUserId : null,
            ActorServiceTokenId = tokenPresent ? actorServiceTokenId : null,
            ActorIpAddress = actorIpAddress,
            ActorUserAgent = actorUserAgent,
            OccurredAtUtc = clock.UtcNow,
            Action = action,
            TargetType = targetType,
            TargetId = targetId,
            MetadataJson = metadataJson,
            Outcome = outcome,
            Reason = reason,
        };
    }
}
