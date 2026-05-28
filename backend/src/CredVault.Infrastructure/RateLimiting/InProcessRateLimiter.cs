using System.Threading.RateLimiting;
using CredVault.Infrastructure.Vault;
using Microsoft.Extensions.Options;
using AppRateLimitLease = CredVault.Application.Abstractions.RateLimiting.RateLimitLease;

namespace CredVault.Infrastructure.RateLimiting;

/// <summary>
/// In-process credential-decryption rate limiter using
/// <see cref="PartitionedRateLimiter.Create{TResource, TPartitionKey}"/>. Default: 60 calls per
/// credential per rolling minute; overridable via <see cref="VaultRateLimitOptions"/>.
/// </summary>
public sealed class InProcessRateLimiter : IInProcessRateLimiter, IAsyncDisposable
{
    private readonly PartitionedRateLimiter<string> _limiter;

    /// <summary>Constructs the limiter from options.</summary>
    public InProcessRateLimiter(IOptions<VaultRateLimitOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var perMinute = Math.Max(1, options.Value.PerCredentialPerMinute);

        _limiter = PartitionedRateLimiter.Create<string, string>(partitionKey =>
            RateLimitPartition.GetFixedWindowLimiter(partitionKey,
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = perMinute,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    AutoReplenishment = true,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                }));
    }

    /// <inheritdoc/>
    public Task<AppRateLimitLease> AcquireAsync(string partitionKey, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(partitionKey);
        cancellationToken.ThrowIfCancellationRequested();

        using var lease = _limiter.AttemptAcquire(partitionKey);
        if (lease.IsAcquired)
            return Task.FromResult(new AppRateLimitLease(true, TimeSpan.Zero));

        var retryAfter = lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan delay)
            ? delay
            : TimeSpan.FromSeconds(1);
        return Task.FromResult(new AppRateLimitLease(false, retryAfter));
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync() => await _limiter.DisposeAsync().ConfigureAwait(false);
}
