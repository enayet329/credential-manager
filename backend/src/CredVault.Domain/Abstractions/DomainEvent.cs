namespace CredVault.Domain.Abstractions;

/// <summary>
/// Base record for all domain events. Domain events are immutable facts about something that has
/// already happened inside an aggregate; handlers in the application layer consume them
/// (audit logging, webhook dispatch, cache invalidation, etc.).
/// </summary>
/// <param name="OccurredAtUtc">The UTC instant at which the event was raised.</param>
public abstract record DomainEvent(DateTime OccurredAtUtc);
