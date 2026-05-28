namespace CredVault.Domain.ServiceTokens.Events;

/// <summary>Raised when a service token is revoked.</summary>
public sealed record ServiceTokenRevoked(Guid ServiceTokenId, DateTime OccurredAtUtc) : DomainEvent(OccurredAtUtc);
