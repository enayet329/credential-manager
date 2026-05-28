using CredVault.Domain.Webhooks;
using Microsoft.EntityFrameworkCore;

namespace CredVault.Infrastructure.Persistence.Repositories;

internal sealed class WebhookDeliveryRepository : IWebhookDeliveryRepository
{
    private readonly CredVaultDbContext _context;
    private readonly IDateTimeProvider _clock;

    public WebhookDeliveryRepository(CredVaultDbContext context, IDateTimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(clock);
        _context = context;
        _clock = clock;
    }

    public void Add(WebhookDelivery delivery)
    {
        ArgumentNullException.ThrowIfNull(delivery);
        _context.WebhookDeliveries.Add(delivery);
    }

    /// <summary>
    /// Atomically claims due deliveries. The UPDATE-OUTPUT pattern with UPDLOCK + READPAST lets many
    /// workers run concurrently — each call returns a disjoint batch.
    /// </summary>
    public async Task<IReadOnlyList<WebhookDelivery>> ClaimDueDeliveriesAsync(int batchSize, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(batchSize);

        var nowParam = new Microsoft.Data.SqlClient.SqlParameter("@now", _clock.UtcNow);
        var sizeParam = new Microsoft.Data.SqlClient.SqlParameter("@batchSize", batchSize);

        // Bump AttemptCount and push NextAttemptAtUtc out by 60s so a peer worker that arrives
        // before this batch is processed doesn't grab the same rows. The caller updates the row
        // again on completion via MarkSucceeded / MarkFailed.
        const string Sql = """
            UPDATE TOP (@batchSize) d
            SET d.AttemptCount = d.AttemptCount + 1,
                d.NextAttemptAtUtc = DATEADD(SECOND, 60, @now)
            OUTPUT inserted.*
            FROM dbo.WebhookDeliveries AS d WITH (UPDLOCK, READPAST, ROWLOCK)
            WHERE d.SucceededAtUtc IS NULL
              AND d.NextAttemptAtUtc IS NOT NULL
              AND d.NextAttemptAtUtc <= @now
            """;

        var rows = await _context.WebhookDeliveries
            .FromSqlRaw(Sql, nowParam, sizeParam)
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rows;
    }
}
