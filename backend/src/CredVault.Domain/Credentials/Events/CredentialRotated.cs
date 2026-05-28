namespace CredVault.Domain.Credentials.Events;

/// <summary>Raised when a credential's secret material is replaced. The previous material is captured in a rotation row.</summary>
public sealed record CredentialRotated(Guid CredentialId, Guid RotatedByUserId, DateTime OccurredAtUtc)
    : DomainEvent(OccurredAtUtc);
