using Microsoft.Extensions.Caching.Hybrid;

namespace CredVault.Infrastructure.Caching;

/// <summary>
/// <see cref="IVaultCache"/> over <see cref="HybridCache"/>. Stampede-protected by design — concurrent
/// requests for a cold key all wait on the same in-flight factory call.
/// </summary>
public sealed class HybridVaultCache : IVaultCache
{
    private readonly HybridCache _cache;

    /// <summary>Constructs the wrapper.</summary>
    public HybridVaultCache(HybridCache cache)
    {
        ArgumentNullException.ThrowIfNull(cache);
        _cache = cache;
    }

    /// <inheritdoc/>
    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, ValueTask<T>> factory,
        TimeSpan? ttl = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(factory);

        HybridCacheEntryOptions? options = null;
        if (ttl is { } expiration)
        {
            options = new HybridCacheEntryOptions
            {
                Expiration = expiration,
                LocalCacheExpiration = expiration < TimeSpan.FromMinutes(1) ? expiration : TimeSpan.FromMinutes(1),
            };
        }

        return await _cache.GetOrCreateAsync(
            key,
            factory,
            options,
            tags,
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        await _cache.RemoveAsync(key, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task RemoveByTagAsync(string tag, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tag);
        await _cache.RemoveByTagAsync(tag, cancellationToken).ConfigureAwait(false);
    }
}
