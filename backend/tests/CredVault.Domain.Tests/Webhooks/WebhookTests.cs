using CredVault.Domain.Webhooks;
using CredVault.Domain.Webhooks.Events;

namespace CredVault.Domain.Tests.Webhooks;

public class WebhookTests
{
    private readonly FakeClock _clock = new();

    private Webhook NewWebhook() =>
        Webhook.Create(
            organizationId: Guid.NewGuid(),
            url: "https://hooks.example.com/x",
            signingSecretReferenceId: Guid.NewGuid(),
            events: [WebhookEventTypes.CredentialCreated, WebhookEventTypes.CredentialRotated],
            clock: _clock);

    [Fact]
    public void Create_succeeds_with_valid_inputs()
    {
        var w = NewWebhook();
        w.Url.Should().Be("https://hooks.example.com/x");
        w.IsActive.Should().BeTrue();
        w.Events.Should().HaveCount(2);
        w.CreatedAtUtc.Should().Be(_clock.UtcNow);
    }

    [Fact]
    public void Create_rejects_empty_org_id() =>
        ((Action)(() => Webhook.Create(Guid.Empty, "https://x.io", Guid.NewGuid(), [WebhookEventTypes.CredentialCreated], _clock)))
            .Should().Throw<DomainException>().WithMessage("*OrganizationId*");

    [Fact]
    public void Create_rejects_empty_secret_reference() =>
        ((Action)(() => Webhook.Create(Guid.NewGuid(), "https://x.io", Guid.Empty, [WebhookEventTypes.CredentialCreated], _clock)))
            .Should().Throw<DomainException>().WithMessage("*SigningSecretReferenceId*");

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_rejects_empty_url(string url) =>
        ((Action)(() => Webhook.Create(Guid.NewGuid(), url, Guid.NewGuid(), [WebhookEventTypes.CredentialCreated], _clock)))
            .Should().Throw<DomainException>().WithMessage("*URL*");

    [Theory]
    [InlineData("ftp://x.io")]
    [InlineData("not-a-url")]
    [InlineData("/relative")]
    public void Create_rejects_non_http_url(string url) =>
        ((Action)(() => Webhook.Create(Guid.NewGuid(), url, Guid.NewGuid(), [WebhookEventTypes.CredentialCreated], _clock)))
            .Should().Throw<DomainException>().WithMessage("*http(s)*");

    [Fact]
    public void Create_rejects_empty_events() =>
        ((Action)(() => Webhook.Create(Guid.NewGuid(), "https://x.io", Guid.NewGuid(), [], _clock)))
            .Should().Throw<DomainException>().WithMessage("*at least one event*");

    [Fact]
    public void Create_rejects_unknown_event() =>
        ((Action)(() => Webhook.Create(Guid.NewGuid(), "https://x.io", Guid.NewGuid(), ["not.an.event"], _clock)))
            .Should().Throw<DomainException>().WithMessage("*Unknown*");

    [Fact]
    public void Create_deduplicates_event_names() =>
        Webhook.Create(Guid.NewGuid(), "https://x.io", Guid.NewGuid(),
            [WebhookEventTypes.CredentialCreated, WebhookEventTypes.CredentialCreated], _clock)
            .Events.Should().ContainSingle();

    [Fact]
    public void Create_rejects_nulls()
    {
        ((Action)(() => Webhook.Create(Guid.NewGuid(), null!, Guid.NewGuid(), [WebhookEventTypes.CredentialCreated], _clock))).Should().Throw<ArgumentNullException>();
        ((Action)(() => Webhook.Create(Guid.NewGuid(), "https://x.io", Guid.NewGuid(), null!, _clock))).Should().Throw<ArgumentNullException>();
        ((Action)(() => Webhook.Create(Guid.NewGuid(), "https://x.io", Guid.NewGuid(), [WebhookEventTypes.CredentialCreated], null!))).Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void UpdateUrl_changes_url()
    {
        var w = NewWebhook();
        w.UpdateUrl("https://other.example.com/hook");
        w.Url.Should().Be("https://other.example.com/hook");
    }

    [Theory]
    [InlineData("")]
    [InlineData("ftp://x.io")]
    public void UpdateUrl_rejects_invalid(string url) =>
        ((Action)(() => NewWebhook().UpdateUrl(url))).Should().Throw<DomainException>();

    [Fact]
    public void UpdateEvents_replaces_subscriptions()
    {
        var w = NewWebhook();
        w.UpdateEvents([WebhookEventTypes.ServiceTokenCreated]);
        w.Events.Should().ContainSingle().Which.Should().Be(WebhookEventTypes.ServiceTokenCreated);
    }

    [Fact]
    public void UpdateEvents_rejects_empty_and_unknown_and_null()
    {
        var w = NewWebhook();
        ((Action)(() => w.UpdateEvents([]))).Should().Throw<DomainException>().WithMessage("*at least one*");
        ((Action)(() => w.UpdateEvents(["bad"]))).Should().Throw<DomainException>().WithMessage("*Unknown*");
        ((Action)(() => w.UpdateEvents(null!))).Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Activate_Deactivate_toggle()
    {
        var w = NewWebhook();
        w.Deactivate();
        w.IsActive.Should().BeFalse();
        w.Activate();
        w.IsActive.Should().BeTrue();
    }

    [Fact]
    public void EventTypes_All_contains_known_events()
    {
        WebhookEventTypes.IsKnown(WebhookEventTypes.CredentialAccessed).Should().BeTrue();
        WebhookEventTypes.IsKnown("nope").Should().BeFalse();
        WebhookEventTypes.All.Should().Contain(WebhookEventTypes.MemberRoleChanged);
    }
}
