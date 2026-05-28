using System.Reflection;
using CredVault.Infrastructure.Vault;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace CredVault.Infrastructure.Tests.Vault;

public class PlaintextCacheZeroingTests
{
    /// <summary>
    /// Captures the inner buffer the cache holds, then forces invalidation and asserts that EVERY byte
    /// has been zeroed in place by the PostEvictionCallback.
    /// </summary>
    [Fact]
    public void PostEvictionCallback_zeros_the_stored_buffer_in_place()
    {
        using var cache = new PlaintextCache(Options.Create(new PlaintextCacheOptions { Enabled = true, Ttl = TimeSpan.FromSeconds(2) }));
        var id = Guid.NewGuid();

        cache.Set(id, [0x11, 0x22, 0x33, 0x44]);

        var memoryCacheField = typeof(PlaintextCache).GetField("_cache", BindingFlags.Instance | BindingFlags.NonPublic);
        memoryCacheField.Should().NotBeNull();
        var memoryCache = (MemoryCache)memoryCacheField!.GetValue(cache)!;

        memoryCache.TryGetValue(id, out byte[]? stored).Should().BeTrue();
        stored.Should().NotBeNull();
        stored!.Should().Equal([0x11, 0x22, 0x33, 0x44]);

        // Force eviction via the public Invalidate API. MemoryCache dispatches the eviction
        // callback to the threadpool, so we spin briefly until the buffer is observed zeroed.
        cache.Invalidate(id);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        while (stopwatch.ElapsedMilliseconds < 2_000 && stored!.Any(b => b != 0))
            Thread.Sleep(10);

        stored!.All(b => b == 0).Should().BeTrue("PostEvictionCallback must wipe stored plaintext");
    }
}
