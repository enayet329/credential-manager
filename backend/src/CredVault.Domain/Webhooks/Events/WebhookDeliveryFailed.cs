namespace CredVault.Domain.Webhooks.Events;

/// <summary>Raised when a webhook delivery fails (non-2xx, network error, timeout).</summary>
public sealed record WebhookDeliveryFailed(
    Guid WebhookDeliveryId,
    Guid WebhookId,
    int AttemptCount,
    int? ResponseStatus,
    string Error,
    DateTime OccurredAtUtc) : DomainEvent(OccurredAtUtc);
