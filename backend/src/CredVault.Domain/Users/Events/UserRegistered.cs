namespace CredVault.Domain.Users.Events;

/// <summary>Raised when a new user account is created.</summary>
/// <param name="UserId">Identity of the newly-created user.</param>
/// <param name="Email">The verified email address (lower-cased).</param>
/// <param name="OccurredAtUtc">UTC timestamp the event was raised.</param>
public sealed record UserRegistered(Guid UserId, string Email, DateTime OccurredAtUtc) : DomainEvent(OccurredAtUtc);
