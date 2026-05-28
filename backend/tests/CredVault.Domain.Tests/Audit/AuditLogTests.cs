using CredVault.Domain.Audit;

namespace CredVault.Domain.Tests.Audit;

public class AuditLogTests
{
    private readonly FakeClock _clock = new();

    [Fact]
    public void Record_with_user_actor_succeeds()
    {
        var orgId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var log = AuditLog.Record(orgId, userId, null, "1.2.3.4", "ua",
            "member.added", "User", userId.ToString(), "{}", AccessOutcome.Success, null, _clock);
        log.ActorUserId.Should().Be(userId);
        log.ActorServiceTokenId.Should().BeNull();
        log.OrganizationId.Should().Be(orgId);
        log.OccurredAtUtc.Should().Be(_clock.UtcNow);
        log.Outcome.Should().Be(AccessOutcome.Success);
    }

    [Fact]
    public void Record_with_token_actor_succeeds()
    {
        var tokenId = Guid.NewGuid();
        var log = AuditLog.Record(Guid.NewGuid(), null, tokenId, "ip", "ua",
            "credential.created", "Credential", Guid.NewGuid().ToString(), "{}", AccessOutcome.Success, null, _clock);
        log.ActorServiceTokenId.Should().Be(tokenId);
        log.ActorUserId.Should().BeNull();
    }

    [Fact]
    public void Record_requires_exactly_one_actor()
    {
        var bothNull = () => AuditLog.Record(Guid.NewGuid(), null, null, "", "", "a", "T", "id", "{}", AccessOutcome.Success, null, _clock);
        var bothSet = () => AuditLog.Record(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "", "", "a", "T", "id", "{}", AccessOutcome.Success, null, _clock);
        var emptyUser = () => AuditLog.Record(Guid.NewGuid(), Guid.Empty, null, "", "", "a", "T", "id", "{}", AccessOutcome.Success, null, _clock);

        bothNull.Should().Throw<DomainException>().WithMessage("*exactly one*");
        bothSet.Should().Throw<DomainException>().WithMessage("*exactly one*");
        emptyUser.Should().Throw<DomainException>().WithMessage("*exactly one*");
    }

    [Fact]
    public void Record_rejects_empty_org() =>
        ((Action)(() => AuditLog.Record(Guid.Empty, Guid.NewGuid(), null, "", "", "a", "T", "id", "{}", AccessOutcome.Success, null, _clock)))
            .Should().Throw<DomainException>().WithMessage("*OrganizationId*");

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Record_rejects_empty_action(string action) =>
        ((Action)(() => AuditLog.Record(Guid.NewGuid(), Guid.NewGuid(), null, "", "", action, "T", "id", "{}", AccessOutcome.Success, null, _clock)))
            .Should().Throw<DomainException>().WithMessage("*Action*");

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Record_rejects_empty_target_type(string targetType) =>
        ((Action)(() => AuditLog.Record(Guid.NewGuid(), Guid.NewGuid(), null, "", "", "a", targetType, "id", "{}", AccessOutcome.Success, null, _clock)))
            .Should().Throw<DomainException>().WithMessage("*TargetType*");

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Record_rejects_empty_target_id(string id) =>
        ((Action)(() => AuditLog.Record(Guid.NewGuid(), Guid.NewGuid(), null, "", "", "a", "T", id, "{}", AccessOutcome.Success, null, _clock)))
            .Should().Throw<DomainException>().WithMessage("*TargetId*");

    [Fact]
    public void Record_rejects_nulls()
    {
        ((Action)(() => AuditLog.Record(Guid.NewGuid(), Guid.NewGuid(), null, null!, "", "a", "T", "id", "{}", AccessOutcome.Success, null, _clock))).Should().Throw<ArgumentNullException>();
        ((Action)(() => AuditLog.Record(Guid.NewGuid(), Guid.NewGuid(), null, "", null!, "a", "T", "id", "{}", AccessOutcome.Success, null, _clock))).Should().Throw<ArgumentNullException>();
        ((Action)(() => AuditLog.Record(Guid.NewGuid(), Guid.NewGuid(), null, "", "", "a", "T", "id", null!, AccessOutcome.Success, null, _clock))).Should().Throw<ArgumentNullException>();
        ((Action)(() => AuditLog.Record(Guid.NewGuid(), Guid.NewGuid(), null, "", "", "a", "T", "id", "{}", AccessOutcome.Success, null, null!))).Should().Throw<ArgumentNullException>();
    }
}
