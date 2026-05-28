namespace CredVault.Domain.ServiceTokens.Events;

/// <summary>Raised the very first time a service token is presented at the auth layer.</summary>
public sealed record ServiceTokenFirstUsed(Guid ServiceTokenId, DateTime OccurredAtUtc) : DomainEvent(OccurredAtUtc);
