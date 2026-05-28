namespace CredVault.Application.Abstractions.Caching;

/// <summary>
/// Thin wrapper over <c>HybridCache</c>. NEVER store decrypted credential values here — the cache is
/// for schemas, service-token metadata, project/env lookups, etc.
/// </summary>
public interface IVaultCache
{
    /// <summary>Returns a cached value, calling <paramref name="factory"/> exactly once per cold key (stampede-safe).</summary>
    Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, ValueTask<T>> factory,
        TimeSpan? ttl = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default);

    /// <summary>Evicts a single entry.</summary>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Evicts every entry stored under <paramref name="tag"/>.</summary>
    Task RemoveByTagAsync(string tag, CancellationToken cancellationToken = default);
}
