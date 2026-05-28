using CredVault.Infrastructure.Caching;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;

namespace CredVault.Infrastructure.Tests.Caching;

public class HybridVaultCacheTests
{
    [Fact]
    public async Task GetOrCreateAsync_invokes_factory_exactly_once_under_stampede()
    {
        var services = new ServiceCollection();
        services.AddHybridCache();
        await using var sp = services.BuildServiceProvider();
        var cache = new HybridVaultCache(sp.GetRequiredService<HybridCache>());

        var factoryCalls = 0;
        var releaseFactories = new TaskCompletionSource<int>();

        async ValueTask<int> Factory(CancellationToken _)
        {
            Interlocked.Increment(ref factoryCalls);
            return await releaseFactories.Task;
        }

        const int callers = 50;
        var tasks = new Task<int>[callers];
        for (var i = 0; i < callers; i++)
            tasks[i] = cache.GetOrCreateAsync<int>($"shared-key", Factory);

        // Give all callers a chance to hit the in-flight slot before releasing the factory.
        await Task.Delay(50);
        releaseFactories.SetResult(42);
        var results = await Task.WhenAll(tasks);

        results.Should().AllSatisfy(value => value.Should().Be(42));
        factoryCalls.Should().Be(1, "HybridCache must coalesce concurrent factory invocations on a cold key");
    }

    [Fact]
    public async Task RemoveAsync_evicts_the_entry()
    {
        var services = new ServiceCollection();
        services.AddHybridCache();
        await using var sp = services.BuildServiceProvider();
        var cache = new HybridVaultCache(sp.GetRequiredService<HybridCache>());

        var calls = 0;
        ValueTask<int> Factory(CancellationToken _) => ValueTask.FromResult(Interlocked.Increment(ref calls));

        (await cache.GetOrCreateAsync<int>("k", Factory)).Should().Be(1);
        await cache.RemoveAsync("k");
        (await cache.GetOrCreateAsync<int>("k", Factory)).Should().Be(2);
    }
}

