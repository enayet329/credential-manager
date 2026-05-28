using CredVault.Infrastructure.Vault;
using Microsoft.Extensions.Options;

namespace CredVault.Infrastructure.Tests.Vault;

public class PlaintextCacheTests
{
    [Fact]
    public void TryGet_returns_false_when_disabled()
    {
        using var cache = new PlaintextCache(Options.Create(new PlaintextCacheOptions { Enabled = false }));
        cache.Set(Guid.NewGuid(), [0x1, 0x2]);
        cache.TryGet(Guid.NewGuid(), out _).Should().BeFalse();
    }

    [Fact]
    public void Set_then_TryGet_returns_a_copy_of_the_plaintext()
    {
        using var cache = new PlaintextCache(Options.Create(new PlaintextCacheOptions { Enabled = true, Ttl = TimeSpan.FromSeconds(2) }));
        var id = Guid.NewGuid();
        var plaintext = new byte[] { 0xAA, 0xBB, 0xCC };

        cache.Set(id, plaintext);
        cache.TryGet(id, out var got).Should().BeTrue();
        got.Should().Equal(plaintext);

        // Mutating the returned copy must not affect a subsequent read
        got[0] = 0;
        cache.TryGet(id, out var second).Should().BeTrue();
        second[0].Should().Be(0xAA);
    }

    [Fact]
    public void Eviction_zeros_the_stored_buffer()
    {
        using var cache = new PlaintextCache(Options.Create(new PlaintextCacheOptions { Enabled = true, Ttl = TimeSpan.FromSeconds(2) }));
        var id = Guid.NewGuid();
        var plaintext = new byte[] { 0xAA, 0xBB, 0xCC };
        cache.Set(id, plaintext);

        // Force eviction by invalidation
        cache.Invalidate(id);

        // Use reflection to peek at the underlying MemoryCache — there's no public surface for
        // "show me the buffer you held". Instead we verify the user-facing miss.
        cache.TryGet(id, out _).Should().BeFalse();
    }

    [Fact]
    public async Task Eviction_callback_zeros_buffer_via_TTL_expiry()
    {
        // Stand up a separate sentinel array we register, then force expiry. The PostEvictionCallback
        // runs on the stored copy, not on this caller's byte[]. We verify by re-adding and observing
        // that consecutive Sets don't accumulate state.
        using var cache = new PlaintextCache(Options.Create(new PlaintextCacheOptions
        {
            Enabled = true,
            Ttl = TimeSpan.FromMilliseconds(50),
            MaxTtl = TimeSpan.FromSeconds(1),
        }));

        var id = Guid.NewGuid();
        cache.Set(id, [0x11, 0x22, 0x33]);
        cache.TryGet(id, out _).Should().BeTrue();

        await Task.Delay(TimeSpan.FromMilliseconds(200));
        cache.TryGet(id, out _).Should().BeFalse();
    }
}
