namespace CredVault.Domain.Credentials.Events;

/// <summary>Raised when a credential is first stored.</summary>
public sealed record CredentialCreated(Guid CredentialId, Guid SupplierId, Guid EnvironmentId, DateTime OccurredAtUtc)
    : DomainEvent(OccurredAtUtc);
