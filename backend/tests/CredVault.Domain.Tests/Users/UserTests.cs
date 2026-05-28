using CredVault.Domain.Users;
using CredVault.Domain.Users.Events;

namespace CredVault.Domain.Tests.Users;

public class UserTests
{
    private readonly FakeClock _clock = new();

    private User NewUser() =>
        User.Register(Email.Create("user@example.com"), "hash", _clock);

    [Fact]
    public void Register_creates_user_and_raises_event()
    {
        var user = NewUser();
        user.Id.Should().NotBe(Guid.Empty);
        user.Email.Value.Should().Be("user@example.com");
        user.PasswordHash.Should().Be("hash");
        user.EmailConfirmed.Should().BeFalse();
        user.MfaEnabled.Should().BeFalse();
        user.FailedLoginAttempts.Should().Be(0);
        user.DomainEvents.OfType<UserRegistered>().Should().ContainSingle()
            .Which.Email.Should().Be("user@example.com");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Register_rejects_empty_password_hash(string hash)
    {
        var act = () => User.Register(Email.Create("a@b.com"), hash, _clock);
        act.Should().Throw<DomainException>().WithMessage("*Password hash*");
    }

    [Fact]
    public void Register_rejects_nulls()
    {
        ((Action)(() => User.Register(null!, "h", _clock))).Should().Throw<ArgumentNullException>();
        ((Action)(() => User.Register(Email.Create("a@b.com"), "h", null!))).Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ConfirmEmail_flips_flag()
    {
        var user = NewUser();
        user.ConfirmEmail();
        user.EmailConfirmed.Should().BeTrue();
    }

    [Fact]
    public void EnableMfa_sets_flag_and_raises_event()
    {
        var user = NewUser();
        var refId = Guid.NewGuid();
        user.EnableMfa(refId, _clock);
        user.MfaEnabled.Should().BeTrue();
        user.MfaSecretReferenceId.Should().Be(refId);
        user.DomainEvents.OfType<UserMfaEnabled>().Should().ContainSingle()
            .Which.MfaSecretReferenceId.Should().Be(refId);
    }

    [Fact]
    public void EnableMfa_rejects_empty_reference()
    {
        var act = () => NewUser().EnableMfa(Guid.Empty, _clock);
        act.Should().Throw<DomainException>().WithMessage("*MFA secret reference*");
    }

    [Fact]
    public void EnableMfa_rejects_when_already_enabled()
    {
        var user = NewUser();
        user.EnableMfa(Guid.NewGuid(), _clock);
        var act = () => user.EnableMfa(Guid.NewGuid(), _clock);
        act.Should().Throw<DomainException>().WithMessage("*already enabled*");
    }

    [Fact]
    public void DisableMfa_clears_state()
    {
        var user = NewUser();
        user.EnableMfa(Guid.NewGuid(), _clock);
        user.DisableMfa();
        user.MfaEnabled.Should().BeFalse();
        user.MfaSecretReferenceId.Should().BeNull();
    }

    [Fact]
    public void DisableMfa_throws_when_not_enabled()
    {
        var act = () => NewUser().DisableMfa();
        act.Should().Throw<DomainException>().WithMessage("*not enabled*");
    }

    [Fact]
    public void RecordSuccessfulLogin_resets_failures()
    {
        var user = NewUser();
        user.RecordFailedLogin(_clock);
        user.RecordFailedLogin(_clock);
        user.RecordSuccessfulLogin(_clock);
        user.FailedLoginAttempts.Should().Be(0);
        user.LastLoginUtc.Should().Be(_clock.UtcNow);
        user.LockoutEndUtc.Should().BeNull();
    }

    [Fact]
    public void RecordFailedLogin_locks_after_threshold()
    {
        var user = NewUser();
        for (var i = 0; i < User.FailedLoginThreshold; i++)
            user.RecordFailedLogin(_clock);

        user.FailedLoginAttempts.Should().Be(User.FailedLoginThreshold);
        user.LockoutEndUtc.Should().Be(_clock.UtcNow + User.LockoutDuration);
        user.IsLockedOut(_clock).Should().BeTrue();
    }

    [Fact]
    public void RecordFailedLogin_below_threshold_does_not_lock()
    {
        var user = NewUser();
        user.RecordFailedLogin(_clock);
        user.LockoutEndUtc.Should().BeNull();
        user.IsLockedOut(_clock).Should().BeFalse();
    }

    [Fact]
    public void IsLockedOut_returns_false_after_lockout_window_passes()
    {
        var user = NewUser();
        for (var i = 0; i < User.FailedLoginThreshold; i++)
            user.RecordFailedLogin(_clock);
        _clock.Advance(User.LockoutDuration + TimeSpan.FromSeconds(1));
        user.IsLockedOut(_clock).Should().BeFalse();
    }

    [Fact]
    public void Null_clock_throws_on_methods()
    {
        var user = NewUser();
        ((Action)(() => user.EnableMfa(Guid.NewGuid(), null!))).Should().Throw<ArgumentNullException>();
        ((Action)(() => user.RecordSuccessfulLogin(null!))).Should().Throw<ArgumentNullException>();
        ((Action)(() => user.RecordFailedLogin(null!))).Should().Throw<ArgumentNullException>();
    }
}
