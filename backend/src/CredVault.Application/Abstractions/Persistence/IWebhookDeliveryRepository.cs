using CredVault.Domain.Webhooks;

namespace CredVault.Application.Abstractions.Persistence;

/// <summary>Persistence operations on <see cref="WebhookDelivery"/>.</summary>
public interface IWebhookDeliveryRepository
{
    /// <summary>Tracks a new delivery row.</summary>
    void Add(WebhookDelivery delivery);

    /// <summary>
    /// Atomically claims up to <paramref name="batchSize"/> deliveries whose
    /// <c>NextAttemptAtUtc &lt;= UtcNow</c> and that aren't already done. Uses
    /// <c>UPDATE … OUTPUT</c> with <c>UPDLOCK, READPAST</c> so workers don't collide.
    /// </summary>
    Task<IReadOnlyList<WebhookDelivery>> ClaimDueDeliveriesAsync(int batchSize, CancellationToken cancellationToken);
}
