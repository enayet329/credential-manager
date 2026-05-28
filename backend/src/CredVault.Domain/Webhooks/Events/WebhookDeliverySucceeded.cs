namespace CredVault.Domain.Webhooks.Events;

/// <summary>Raised when a webhook delivery returns a 2xx response.</summary>
public sealed record WebhookDeliverySucceeded(
    Guid WebhookDeliveryId,
    Guid WebhookId,
    int ResponseStatus,
    DateTime OccurredAtUtc) : DomainEvent(OccurredAtUtc);
