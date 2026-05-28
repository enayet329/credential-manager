namespace CredVault.Domain.Credentials.Events;

/// <summary>
/// Raised every time a credential is successfully decrypted. High volume — handlers must be lightweight
/// (the dedicated <c>CredentialAccessLog</c> row is the source of truth for audit dashboards).
/// </summary>
public sealed record CredentialAccessed(
    Guid CredentialId,
    ActorType ActorType,
    Guid ActorId,
    AccessMethod AccessMethod,
    DateTime OccurredAtUtc) : DomainEvent(OccurredAtUtc);
