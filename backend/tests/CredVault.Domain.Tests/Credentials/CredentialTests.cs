using CredVault.Domain.Credentials;
using CredVault.Domain.Credentials.Events;

namespace CredVault.Domain.Tests.Credentials;

public class CredentialTests
{
    private readonly FakeClock _clock = new();

    private static CredentialEnvelope Envelope(byte marker = 0x01) =>
        new(Bytes.Repeat(marker, 32), Bytes.Repeat(marker, 32),
            Bytes.Repeat(marker, CredentialEnvelope.NonceLength),
            Bytes.Repeat(marker, CredentialEnvelope.AuthTagLength),
            KekVersion: 1, MaskedPreview: $"sk-***{marker:x2}");

    private Credential NewCredential(DateTime? expires = null) =>
        Credential.Create(
            supplierId: Guid.NewGuid(),
            environmentId: Guid.NewGuid(),
            name: "Primary",
            slug: Slug.Create("primary-key"),
            envelope: Envelope(),
            schemaVersion: 1,
            expiresAtUtc: expires,
            clock: _clock);

    [Fact]
    public void Create_sets_state_and_raises_event()
    {
        var c = NewCredential();
        c.Name.Should().Be("Primary");
        c.Slug.Value.Should().Be("primary-key");
        c.CreatedAtUtc.Should().Be(_clock.UtcNow);
        c.RotatedAtUtc.Should().Be(_clock.UtcNow);
        c.AccessCount.Should().Be(0);
        c.IsRevoked.Should().BeFalse();
        c.Rotations.Should().BeEmpty();
        c.MaskedPreview.Should().StartWith("sk-***");
        c.DomainEvents.OfType<CredentialCreated>().Should().ContainSingle()
            .Which.CredentialId.Should().Be(c.Id);
    }

    [Fact]
    public void Create_rejects_empty_supplier_id() =>
        ((Action)(() => Credential.Create(Guid.Empty, Guid.NewGuid(), "n", Slug.Create("abc"), Envelope(), 1, null, _clock)))
            .Should().Throw<DomainException>().WithMessage("*SupplierId*");

    [Fact]
    public void Create_rejects_empty_environment_id() =>
        ((Action)(() => Credential.Create(Guid.NewGuid(), Guid.Empty, "n", Slug.Create("abc"), Envelope(), 1, null, _clock)))
            .Should().Throw<DomainException>().WithMessage("*EnvironmentId*");

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_rejects_empty_name(string n) =>
        ((Action)(() => Credential.Create(Guid.NewGuid(), Guid.NewGuid(), n, Slug.Create("abc"), Envelope(), 1, null, _clock)))
            .Should().Throw<DomainException>();

    [Fact]
    public void Create_rejects_long_name() =>
        ((Action)(() => Credential.Create(Guid.NewGuid(), Guid.NewGuid(), new string('a', 101), Slug.Create("abc"), Envelope(), 1, null, _clock)))
            .Should().Throw<DomainException>();

    [Fact]
    public void Create_rejects_invalid_schema_version() =>
        ((Action)(() => Credential.Create(Guid.NewGuid(), Guid.NewGuid(), "n", Slug.Create("abc"), Envelope(), 0, null, _clock)))
            .Should().Throw<DomainException>().WithMessage("*Schema version*");

    [Fact]
    public void Create_rejects_past_expiry() =>
        ((Action)(() => Credential.Create(Guid.NewGuid(), Guid.NewGuid(), "n", Slug.Create("abc"), Envelope(), 1, _clock.UtcNow.AddSeconds(-1), _clock)))
            .Should().Throw<DomainException>().WithMessage("*future*");

    [Fact]
    public void Create_rejects_nulls()
    {
        ((Action)(() => Credential.Create(Guid.NewGuid(), Guid.NewGuid(), "n", null!, Envelope(), 1, null, _clock))).Should().Throw<ArgumentNullException>();
        ((Action)(() => Credential.Create(Guid.NewGuid(), Guid.NewGuid(), "n", Slug.Create("abc"), null!, 1, null, _clock))).Should().Throw<ArgumentNullException>();
        ((Action)(() => Credential.Create(Guid.NewGuid(), Guid.NewGuid(), "n", Slug.Create("abc"), Envelope(), 1, null, null!))).Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Rotate_appends_history_and_updates_material()
    {
        var c = NewCredential();
        _clock.Advance(TimeSpan.FromMinutes(5));
        var actor = Guid.NewGuid();
        c.Rotate(Envelope(0x99), actor, "scheduled", _clock.UtcNow.AddYears(1), _clock);

        c.Rotations.Should().ContainSingle();
        c.Rotations[0].RotatedByUserId.Should().Be(actor);
        c.Rotations[0].Reason.Should().Be("scheduled");
        c.Rotations[0].PreviousKekVersion.Should().Be(1);
        c.MaskedPreview.Should().Contain("99");
        c.RotatedAtUtc.Should().Be(_clock.UtcNow);
        c.ExpiresAtUtc.Should().NotBeNull();
        c.DomainEvents.OfType<CredentialRotated>().Should().ContainSingle();
    }

    [Fact]
    public void Rotate_rejects_when_revoked()
    {
        var c = NewCredential();
        c.Revoke(Guid.NewGuid(), _clock);
        ((Action)(() => c.Rotate(Envelope(0x05), Guid.NewGuid(), null, null, _clock)))
            .Should().Throw<DomainException>().WithMessage("*revoked*");
    }

    [Fact]
    public void Rotate_rejects_empty_actor() =>
        ((Action)(() => NewCredential().Rotate(Envelope(0x05), Guid.Empty, null, null, _clock)))
            .Should().Throw<DomainException>().WithMessage("*RotatedByUserId*");

    [Fact]
    public void Rotate_rejects_past_expiry() =>
        ((Action)(() => NewCredential().Rotate(Envelope(0x05), Guid.NewGuid(), null, _clock.UtcNow.AddSeconds(-1), _clock)))
            .Should().Throw<DomainException>().WithMessage("*future*");

    [Fact]
    public void Rotate_rejects_nulls()
    {
        var c = NewCredential();
        ((Action)(() => c.Rotate(null!, Guid.NewGuid(), null, null, _clock))).Should().Throw<ArgumentNullException>();
        ((Action)(() => c.Rotate(Envelope(0x05), Guid.NewGuid(), null, null, null!))).Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void RecordAccess_updates_counters_and_emits_event()
    {
        var c = NewCredential();
        var actor = Guid.NewGuid();
        _clock.Advance(TimeSpan.FromSeconds(30));
        c.RecordAccess(ActorType.User, actor, AccessMethod.UI, _clock);
        c.AccessCount.Should().Be(1);
        c.LastAccessedAtUtc.Should().Be(_clock.UtcNow);
        c.DomainEvents.OfType<CredentialAccessed>().Should().ContainSingle()
            .Which.ActorId.Should().Be(actor);
    }

    [Fact]
    public void RecordAccess_rejects_empty_actor() =>
        ((Action)(() => NewCredential().RecordAccess(ActorType.User, Guid.Empty, AccessMethod.UI, _clock)))
            .Should().Throw<DomainException>().WithMessage("*ActorId*");

    [Fact]
    public void RecordAccess_rejects_when_revoked()
    {
        var c = NewCredential();
        c.Revoke(Guid.NewGuid(), _clock);
        ((Action)(() => c.RecordAccess(ActorType.User, Guid.NewGuid(), AccessMethod.UI, _clock)))
            .Should().Throw<DomainException>().WithMessage("*revoked*");
    }

    [Fact]
    public void RecordAccess_rejects_null_clock() =>
        ((Action)(() => NewCredential().RecordAccess(ActorType.User, Guid.NewGuid(), AccessMethod.UI, null!)))
            .Should().Throw<ArgumentNullException>();

    [Fact]
    public void Revoke_marks_revoked_and_emits_event()
    {
        var c = NewCredential();
        var actor = Guid.NewGuid();
        c.Revoke(actor, _clock);
        c.IsRevoked.Should().BeTrue();
        c.RevokedByUserId.Should().Be(actor);
        c.RevokedAtUtc.Should().Be(_clock.UtcNow);
        c.DomainEvents.OfType<CredentialRevoked>().Should().ContainSingle();
    }

    [Fact]
    public void Revoke_is_not_idempotent_when_already_revoked()
    {
        var c = NewCredential();
        c.Revoke(Guid.NewGuid(), _clock);
        ((Action)(() => c.Revoke(Guid.NewGuid(), _clock))).Should().Throw<DomainException>();
    }

    [Fact]
    public void Revoke_rejects_empty_actor() =>
        ((Action)(() => NewCredential().Revoke(Guid.Empty, _clock)))
            .Should().Throw<DomainException>().WithMessage("*RevokedByUserId*");

    [Fact]
    public void Revoke_rejects_null_clock() =>
        ((Action)(() => NewCredential().Revoke(Guid.NewGuid(), null!))).Should().Throw<ArgumentNullException>();

    [Fact]
    public void IsExpired_handles_unset_and_past_and_future()
    {
        NewCredential().IsExpired(_clock).Should().BeFalse();

        var withFuture = NewCredential(_clock.UtcNow.AddHours(1));
        withFuture.IsExpired(_clock).Should().BeFalse();

        _clock.Advance(TimeSpan.FromHours(2));
        withFuture.IsExpired(_clock).Should().BeTrue();
    }
}
