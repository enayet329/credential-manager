using CredVault.Domain.ServiceTokens;
using CredVault.Domain.ServiceTokens.Events;

namespace CredVault.Domain.Tests.ServiceTokens;

public class ServiceTokenTests
{
    private readonly FakeClock _clock = new();

    private ServiceToken NewToken(DateTime? expires = null) =>
        ServiceToken.Create(
            organizationId: Guid.NewGuid(),
            projectId: null,
            hmacHash: Bytes.Repeat(0xAB, 32),
            label: "ci-pipeline",
            scopesJson: "[]",
            createdByUserId: Guid.NewGuid(),
            expiresAtUtc: expires,
            clock: _clock);

    [Fact]
    public void Create_sets_prefix_and_state_and_raises_event()
    {
        var t = NewToken();
        t.Prefix.Should().Be(ServiceToken.LivePrefix);
        t.RevokedAtUtc.Should().BeNull();
        t.LastUsedAtUtc.Should().BeNull();
        t.IsActive(_clock).Should().BeTrue();
        t.DomainEvents.OfType<ServiceTokenCreated>().Should().ContainSingle();
    }

    [Fact]
    public void Create_rejects_empty_org_id() =>
        ((Action)(() => ServiceToken.Create(Guid.Empty, null, Bytes.Repeat(0x1, 8), "x", "[]", Guid.NewGuid(), null, _clock)))
            .Should().Throw<DomainException>().WithMessage("*OrganizationId*");

    [Fact]
    public void Create_rejects_empty_creator() =>
        ((Action)(() => ServiceToken.Create(Guid.NewGuid(), null, Bytes.Repeat(0x1, 8), "x", "[]", Guid.Empty, null, _clock)))
            .Should().Throw<DomainException>().WithMessage("*CreatedByUserId*");

    [Fact]
    public void Create_rejects_empty_hash() =>
        ((Action)(() => ServiceToken.Create(Guid.NewGuid(), null, [], "x", "[]", Guid.NewGuid(), null, _clock)))
            .Should().Throw<DomainException>().WithMessage("*HmacHash*");

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_rejects_empty_label(string label) =>
        ((Action)(() => ServiceToken.Create(Guid.NewGuid(), null, Bytes.Repeat(0x1, 8), label, "[]", Guid.NewGuid(), null, _clock)))
            .Should().Throw<DomainException>();

    [Fact]
    public void Create_rejects_long_label() =>
        ((Action)(() => ServiceToken.Create(Guid.NewGuid(), null, Bytes.Repeat(0x1, 8), new string('a', 101), "[]", Guid.NewGuid(), null, _clock)))
            .Should().Throw<DomainException>();

    [Fact]
    public void Create_rejects_past_expiry() =>
        ((Action)(() => ServiceToken.Create(Guid.NewGuid(), null, Bytes.Repeat(0x1, 8), "x", "[]", Guid.NewGuid(), _clock.UtcNow.AddSeconds(-1), _clock)))
            .Should().Throw<DomainException>().WithMessage("*future*");

    [Fact]
    public void Create_rejects_nulls()
    {
        ((Action)(() => ServiceToken.Create(Guid.NewGuid(), null, null!, "x", "[]", Guid.NewGuid(), null, _clock))).Should().Throw<ArgumentNullException>();
        ((Action)(() => ServiceToken.Create(Guid.NewGuid(), null, Bytes.Repeat(0x1, 8), "x", null!, Guid.NewGuid(), null, _clock))).Should().Throw<ArgumentNullException>();
        ((Action)(() => ServiceToken.Create(Guid.NewGuid(), null, Bytes.Repeat(0x1, 8), "x", "[]", Guid.NewGuid(), null, null!))).Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void RecordUsage_first_call_raises_FirstUsed_event()
    {
        var t = NewToken();
        t.RecordUsage(_clock);
        t.LastUsedAtUtc.Should().Be(_clock.UtcNow);
        t.DomainEvents.OfType<ServiceTokenFirstUsed>().Should().ContainSingle();
    }

    [Fact]
    public void RecordUsage_subsequent_calls_do_not_re_raise_FirstUsed()
    {
        var t = NewToken();
        t.RecordUsage(_clock);
        _clock.Advance(TimeSpan.FromMinutes(1));
        t.RecordUsage(_clock);
        t.DomainEvents.OfType<ServiceTokenFirstUsed>().Should().ContainSingle();
        t.LastUsedAtUtc.Should().Be(_clock.UtcNow);
    }

    [Fact]
    public void RecordUsage_rejects_revoked_token()
    {
        var t = NewToken();
        t.Revoke(_clock);
        ((Action)(() => t.RecordUsage(_clock))).Should().Throw<DomainException>().WithMessage("*revoked*");
    }

    [Fact]
    public void RecordUsage_rejects_expired_token()
    {
        var t = NewToken(_clock.UtcNow.AddMinutes(1));
        _clock.Advance(TimeSpan.FromMinutes(2));
        ((Action)(() => t.RecordUsage(_clock))).Should().Throw<DomainException>().WithMessage("*expired*");
        t.IsActive(_clock).Should().BeFalse();
    }

    [Fact]
    public void Revoke_emits_event_and_blocks_further_revocations()
    {
        var t = NewToken();
        t.Revoke(_clock);
        t.RevokedAtUtc.Should().Be(_clock.UtcNow);
        t.IsActive(_clock).Should().BeFalse();
        t.DomainEvents.OfType<ServiceTokenRevoked>().Should().ContainSingle();
        ((Action)(() => t.Revoke(_clock))).Should().Throw<DomainException>().WithMessage("*already revoked*");
    }

    [Fact]
    public void Rename_updates_label()
    {
        var t = NewToken();
        t.Rename(" Renamed ");
        t.Label.Should().Be("Renamed");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Rename_rejects_empty(string label) =>
        ((Action)(() => NewToken().Rename(label))).Should().Throw<DomainException>();

    [Fact]
    public void Rename_rejects_long() =>
        ((Action)(() => NewToken().Rename(new string('a', 101)))).Should().Throw<DomainException>();

    [Fact]
    public void UpdateIpAllowlist_round_trips()
    {
        var t = NewToken();
        t.UpdateIpAllowlist("[\"10.0.0.0/24\"]");
        t.IpAllowlistJson.Should().Be("[\"10.0.0.0/24\"]");
        t.UpdateIpAllowlist(null);
        t.IpAllowlistJson.Should().BeNull();
    }

    [Fact]
    public void Null_clock_throws_on_methods()
    {
        var t = NewToken();
        ((Action)(() => t.RecordUsage(null!))).Should().Throw<ArgumentNullException>();
        ((Action)(() => t.Revoke(null!))).Should().Throw<ArgumentNullException>();
        ((Action)(() => t.IsActive(null!))).Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Scope_record_round_trips_props()
    {
        var s = new ServiceTokenScope(Slug.Create("app"), Slug.Create("prod"), ServiceTokenPermission.Write);
        s.Permission.Should().Be(ServiceTokenPermission.Write);
        s.ProjectSlug.Value.Should().Be("app");
        s.EnvSlug.Value.Should().Be("prod");
    }
}
