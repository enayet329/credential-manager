using System.Security.Cryptography;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace CredVault.Infrastructure.Vault;

/// <summary>
/// Short-lived per-credential plaintext cache. Stores a <c>byte[]</c> copy of the plaintext and zeros
/// it via <see cref="CryptographicOperations.ZeroMemory(Span{byte})"/> when the entry is evicted.
/// </summary>
public sealed class PlaintextCache : IDisposable
{
    private readonly MemoryCache _cache;
    private readonly PlaintextCacheOptions _options;

    /// <summary>Constructs the cache.</summary>
    public PlaintextCache(IOptions<PlaintextCacheOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
        _cache = new MemoryCache(new MemoryCacheOptions());
    }

    /// <summary>Whether the cache is enabled for this process.</summary>
    public bool Enabled => _options.Enabled;

    /// <summary>Looks up a cached entry. Returns a copy so the caller can dispose without racing eviction.</summary>
    public bool TryGet(Guid credentialId, out byte[] plaintextCopy)
    {
        plaintextCopy = [];
        if (!_options.Enabled) return false;
        if (!_cache.TryGetValue(credentialId, out byte[]? stored) || stored is null) return false;

        // Return a copy so eviction's ZeroMemory can't tamper with the caller's data.
        plaintextCopy = new byte[stored.Length];
        Buffer.BlockCopy(stored, 0, plaintextCopy, 0, stored.Length);
        return true;
    }

    /// <summary>Stores <paramref name="plaintext"/> for up to <see cref="PlaintextCacheOptions.Ttl"/> seconds.</summary>
    public void Set(Guid credentialId, byte[] plaintext)
    {
        ArgumentNullException.ThrowIfNull(plaintext);
        if (!_options.Enabled) return;

        var ttl = _options.Ttl < _options.MaxTtl ? _options.Ttl : _options.MaxTtl;

        // Store an internal copy that we own; we can safely zero it on eviction.
        var owned = new byte[plaintext.Length];
        Buffer.BlockCopy(plaintext, 0, owned, 0, plaintext.Length);

        var entryOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl,
            Priority = CacheItemPriority.Low,
        };
        entryOptions.PostEvictionCallbacks.Add(new PostEvictionCallbackRegistration
        {
            EvictionCallback = OnEvicted,
            State = null,
        });

        _cache.Set(credentialId, owned, entryOptions);
    }

    /// <summary>Forces eviction. Used when a credential is rotated or revoked.</summary>
    public void Invalidate(Guid credentialId) => _cache.Remove(credentialId);

    private static void OnEvicted(object key, object? value, EvictionReason reason, object? state)
    {
        if (value is byte[] bytes)
            CryptographicOperations.ZeroMemory(bytes);
    }

    /// <inheritdoc/>
    public void Dispose() => _cache.Dispose();
}
