using CredVault.Domain.Credentials;

namespace CredVault.Domain.Tests.Credentials;

public class CredentialAccessLogTests
{
    private readonly FakeClock _clock = new();

    [Fact]
    public void Record_creates_immutable_row()
    {
        var credId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var log = CredentialAccessLog.Record(credId, ActorType.ServiceToken, actorId, "10.0.0.1", "ua", AccessMethod.Cli, AccessOutcome.Success, _clock);
        log.CredentialId.Should().Be(credId);
        log.ActorId.Should().Be(actorId);
        log.ActorType.Should().Be(ActorType.ServiceToken);
        log.IpAddress.Should().Be("10.0.0.1");
        log.UserAgent.Should().Be("ua");
        log.AccessMethod.Should().Be(AccessMethod.Cli);
        log.Outcome.Should().Be(AccessOutcome.Success);
        log.AccessedAtUtc.Should().Be(_clock.UtcNow);
        log.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Record_rejects_empty_credential_id() =>
        ((Action)(() => CredentialAccessLog.Record(Guid.Empty, ActorType.User, Guid.NewGuid(), "ip", "ua", AccessMethod.UI, AccessOutcome.Success, _clock)))
            .Should().Throw<DomainException>().WithMessage("*CredentialId*");

    [Fact]
    public void Record_rejects_empty_actor_id() =>
        ((Action)(() => CredentialAccessLog.Record(Guid.NewGuid(), ActorType.User, Guid.Empty, "ip", "ua", AccessMethod.UI, AccessOutcome.Success, _clock)))
            .Should().Throw<DomainException>().WithMessage("*ActorId*");

    [Fact]
    public void Record_rejects_nulls()
    {
        ((Action)(() => CredentialAccessLog.Record(Guid.NewGuid(), ActorType.User, Guid.NewGuid(), null!, "ua", AccessMethod.UI, AccessOutcome.Success, _clock))).Should().Throw<ArgumentNullException>();
        ((Action)(() => CredentialAccessLog.Record(Guid.NewGuid(), ActorType.User, Guid.NewGuid(), "ip", null!, AccessMethod.UI, AccessOutcome.Success, _clock))).Should().Throw<ArgumentNullException>();
        ((Action)(() => CredentialAccessLog.Record(Guid.NewGuid(), ActorType.User, Guid.NewGuid(), "ip", "ua", AccessMethod.UI, AccessOutcome.Success, null!))).Should().Throw<ArgumentNullException>();
    }
}
