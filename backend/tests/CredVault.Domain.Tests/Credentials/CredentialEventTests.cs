using CredVault.Domain.Credentials.Events;

namespace CredVault.Domain.Tests.Credentials;

public class CredentialEventTests
{
    [Fact]
    public void CredentialExpiringSoon_carries_payload()
    {
        var now = DateTime.UtcNow;
        var expiry = now.AddDays(7);
        var credId = Guid.NewGuid();
        var evt = new CredentialExpiringSoon(credId, expiry, DaysRemaining: 7, OccurredAtUtc: now);
        evt.CredentialId.Should().Be(credId);
        evt.ExpiresAtUtc.Should().Be(expiry);
        evt.DaysRemaining.Should().Be(7);
        evt.OccurredAtUtc.Should().Be(now);
    }
}
