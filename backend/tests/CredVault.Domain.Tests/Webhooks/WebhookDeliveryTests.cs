using CredVault.Domain.Webhooks;
using CredVault.Domain.Webhooks.Events;

namespace CredVault.Domain.Tests.Webhooks;

public class WebhookDeliveryTests
{
    private readonly FakeClock _clock = new();

    private WebhookDelivery NewDelivery() =>
        WebhookDelivery.Create(Guid.NewGuid(), WebhookEventTypes.CredentialCreated, "{}", _clock);

    [Fact]
    public void Create_initializes_attempt_zero_due_now()
    {
        var d = NewDelivery();
        d.AttemptCount.Should().Be(0);
        d.NextAttemptAtUtc.Should().Be(_clock.UtcNow);
        d.SucceededAtUtc.Should().BeNull();
    }

    [Fact]
    public void Create_rejects_empty_webhook_id() =>
        ((Action)(() => WebhookDelivery.Create(Guid.Empty, WebhookEventTypes.CredentialCreated, "{}", _clock)))
            .Should().Throw<DomainException>().WithMessage("*WebhookId*");

    [Fact]
    public void Create_rejects_unknown_event() =>
        ((Action)(() => WebhookDelivery.Create(Guid.NewGuid(), "nope", "{}", _clock)))
            .Should().Throw<DomainException>().WithMessage("*Unknown*");

    [Fact]
    public void Create_rejects_nulls()
    {
        ((Action)(() => WebhookDelivery.Create(Guid.NewGuid(), WebhookEventTypes.CredentialCreated, null!, _clock))).Should().Throw<ArgumentNullException>();
        ((Action)(() => WebhookDelivery.Create(Guid.NewGuid(), WebhookEventTypes.CredentialCreated, "{}", null!))).Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void MarkSucceeded_sets_state_and_emits_event()
    {
        var d = NewDelivery();
        d.MarkSucceeded(204, _clock);
        d.SucceededAtUtc.Should().Be(_clock.UtcNow);
        d.AttemptCount.Should().Be(1);
        d.LastResponseStatus.Should().Be(204);
        d.NextAttemptAtUtc.Should().BeNull();
        d.DomainEvents.OfType<WebhookDeliverySucceeded>().Should().ContainSingle();
    }

    [Fact]
    public void MarkSucceeded_twice_throws()
    {
        var d = NewDelivery();
        d.MarkSucceeded(200, _clock);
        ((Action)(() => d.MarkSucceeded(200, _clock))).Should().Throw<DomainException>();
    }

    [Fact]
    public void MarkFailed_increments_and_emits_event()
    {
        var d = NewDelivery();
        d.MarkFailed(500, "boom", _clock.UtcNow.AddMinutes(1), _clock);
        d.AttemptCount.Should().Be(1);
        d.LastResponseStatus.Should().Be(500);
        d.LastError.Should().Be("boom");
        d.NextAttemptAtUtc.Should().NotBeNull();
        d.DomainEvents.OfType<WebhookDeliveryFailed>().Should().ContainSingle();
    }

    [Fact]
    public void MarkFailed_with_null_next_means_exhausted()
    {
        var d = NewDelivery();
        d.MarkFailed(null, "timeout", null, _clock);
        d.NextAttemptAtUtc.Should().BeNull();
        d.LastResponseStatus.Should().BeNull();
    }

    [Fact]
    public void MarkFailed_after_success_throws()
    {
        var d = NewDelivery();
        d.MarkSucceeded(200, _clock);
        ((Action)(() => d.MarkFailed(500, "boom", null, _clock))).Should().Throw<DomainException>();
    }

    [Fact]
    public void Null_arguments_throw()
    {
        var d = NewDelivery();
        ((Action)(() => d.MarkFailed(null, null!, null, _clock))).Should().Throw<ArgumentNullException>();
        ((Action)(() => d.MarkFailed(null, "x", null, null!))).Should().Throw<ArgumentNullException>();
        ((Action)(() => d.MarkSucceeded(200, null!))).Should().Throw<ArgumentNullException>();
    }
}
