using CredVault.Domain.Webhooks.Events;

namespace CredVault.Domain.Webhooks;

/// <summary>
/// One attempt to deliver a webhook payload. Retried with exponential backoff via <see cref="NextAttemptAtUtc"/>
/// until it succeeds or the retry budget is exhausted.
/// </summary>
public sealed class WebhookDelivery : Entity
{
    /// <summary>FK to the parent <see cref="Webhook"/>.</summary>
    public Guid WebhookId { get; private init; }

    /// <summary>The event name being delivered (one of <see cref="WebhookEventTypes"/>).</summary>
    public string EventType { get; private init; } = string.Empty;

    /// <summary>The JSON payload body. Domain treats it as opaque.</summary>
    public string PayloadJson { get; private init; } = "{}";

    /// <summary>How many times delivery has been attempted (0 before the first attempt).</summary>
    public int AttemptCount { get; private set; }

    /// <summary>When the next attempt should run. <c>null</c> when delivery is terminal (succeeded or exhausted).</summary>
    public DateTime? NextAttemptAtUtc { get; private set; }

    /// <summary>UTC instant of the successful delivery, if any.</summary>
    public DateTime? SucceededAtUtc { get; private set; }

    /// <summary>HTTP status code of the most recent attempt, if a response was received.</summary>
    public int? LastResponseStatus { get; private set; }

    /// <summary>Error message from the most recent failed attempt.</summary>
    public string? LastError { get; private set; }

    private WebhookDelivery() { }

    /// <summary>Creates a new pending delivery scheduled to run immediately.</summary>
    public static WebhookDelivery Create(
        Guid webhookId,
        string eventType,
        string payloadJson,
        IDateTimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(payloadJson);
        ArgumentNullException.ThrowIfNull(clock);
        if (webhookId == Guid.Empty)
            throw new DomainException("WebhookId must not be empty.");
        if (!WebhookEventTypes.IsKnown(eventType))
            throw new DomainException($"Unknown webhook event '{eventType}'.");

        return new WebhookDelivery
        {
            Id = Guid.NewGuid(),
            WebhookId = webhookId,
            EventType = eventType,
            PayloadJson = payloadJson,
            AttemptCount = 0,
            NextAttemptAtUtc = clock.UtcNow,
        };
    }

    /// <summary>Marks the delivery as successfully delivered. Emits <see cref="WebhookDeliverySucceeded"/>.</summary>
    public void MarkSucceeded(int responseStatus, IDateTimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        if (SucceededAtUtc is not null)
            throw new DomainException("Delivery has already succeeded.");
        var now = clock.UtcNow;
        AttemptCount++;
        LastResponseStatus = responseStatus;
        LastError = null;
        NextAttemptAtUtc = null;
        SucceededAtUtc = now;
        Raise(new WebhookDeliverySucceeded(Id, WebhookId, responseStatus, now));
    }

    /// <summary>
    /// Marks the delivery as failed. <paramref name="nextAttemptAtUtc"/> = <c>null</c> means the retry
    /// budget is exhausted. Emits <see cref="WebhookDeliveryFailed"/>.
    /// </summary>
    public void MarkFailed(int? responseStatus, string error, DateTime? nextAttemptAtUtc, IDateTimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(error);
        ArgumentNullException.ThrowIfNull(clock);
        if (SucceededAtUtc is not null)
            throw new DomainException("Delivery has already succeeded.");

        var now = clock.UtcNow;
        AttemptCount++;
        LastResponseStatus = responseStatus;
        LastError = error;
        NextAttemptAtUtc = nextAttemptAtUtc;
        Raise(new WebhookDeliveryFailed(Id, WebhookId, AttemptCount, responseStatus, error, now));
    }
}
