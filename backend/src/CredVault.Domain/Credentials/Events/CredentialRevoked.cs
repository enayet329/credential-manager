namespace CredVault.Domain.Credentials.Events;

/// <summary>Raised when a credential is marked revoked. Revoked credentials remain in the database for audit but can never be decrypted again.</summary>
public sealed record CredentialRevoked(Guid CredentialId, Guid RevokedByUserId, DateTime OccurredAtUtc)
    : DomainEvent(OccurredAtUtc);
