namespace CredVault.Domain.Credentials.Events;

/// <summary>
/// Raised by the background expiry-reminder job at 30, 7, and 1 days before <c>ExpiresAtUtc</c>.
/// </summary>
/// <param name="CredentialId">Identity of the credential about to expire.</param>
/// <param name="ExpiresAtUtc">When the credential is scheduled to expire.</param>
/// <param name="DaysRemaining">Notification window in days (one of 30, 7, 1).</param>
/// <param name="OccurredAtUtc">UTC timestamp the event was raised.</param>
public sealed record CredentialExpiringSoon(
    Guid CredentialId,
    DateTime ExpiresAtUtc,
    int DaysRemaining,
    DateTime OccurredAtUtc) : DomainEvent(OccurredAtUtc);
