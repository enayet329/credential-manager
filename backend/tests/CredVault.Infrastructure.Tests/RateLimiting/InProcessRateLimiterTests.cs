using CredVault.Infrastructure.RateLimiting;
using CredVault.Infrastructure.Vault;
using Microsoft.Extensions.Options;

namespace CredVault.Infrastructure.Tests.RateLimiting;

public class InProcessRateLimiterTests
{
    [Fact]
    public async Task Acquires_up_to_the_limit_and_denies_after()
    {
        await using var limiter = new InProcessRateLimiter(
            Options.Create(new VaultRateLimitOptions { PerCredentialPerMinute = 3 }));
        var key = $"cred:{Guid.NewGuid()}";

        for (var i = 0; i < 3; i++)
            (await limiter.AcquireAsync(key)).IsAcquired.Should().BeTrue();

        var denied = await limiter.AcquireAsync(key);
        denied.IsAcquired.Should().BeFalse();
    }

    [Fact]
    public async Task Different_partitions_have_independent_budgets()
    {
        await using var limiter = new InProcessRateLimiter(
            Options.Create(new VaultRateLimitOptions { PerCredentialPerMinute = 1 }));

        (await limiter.AcquireAsync("cred:a")).IsAcquired.Should().BeTrue();
        (await limiter.AcquireAsync("cred:b")).IsAcquired.Should().BeTrue();
        (await limiter.AcquireAsync("cred:a")).IsAcquired.Should().BeFalse();
    }
}
