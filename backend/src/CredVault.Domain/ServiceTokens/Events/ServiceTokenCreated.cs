namespace CredVault.Domain.ServiceTokens.Events;

/// <summary>Raised when a service token is minted.</summary>
public sealed record ServiceTokenCreated(
    Guid ServiceTokenId,
    Guid OrganizationId,
    Guid? ProjectId,
    Guid CreatedByUserId,
    DateTime OccurredAtUtc) : DomainEvent(OccurredAtUtc);
