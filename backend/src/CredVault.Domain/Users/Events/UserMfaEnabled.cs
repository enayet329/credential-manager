namespace CredVault.Domain.Users.Events;

/// <summary>Raised when a user activates multi-factor authentication.</summary>
/// <param name="UserId">Identity of the user.</param>
/// <param name="MfaSecretReferenceId">Pointer to the encrypted MFA-secret row in the secrets table.</param>
/// <param name="OccurredAtUtc">UTC timestamp the event was raised.</param>
public sealed record UserMfaEnabled(Guid UserId, Guid MfaSecretReferenceId, DateTime OccurredAtUtc)
    : DomainEvent(OccurredAtUtc);
