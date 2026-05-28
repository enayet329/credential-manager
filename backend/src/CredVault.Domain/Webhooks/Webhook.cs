namespace CredVault.Domain.Webhooks;

/// <summary>
/// A registered outbound webhook for an organisation. Webhooks are signed with an HMAC computed from
/// a secret stored encrypted via <see cref="SigningSecretReferenceId"/>.
/// </summary>
public sealed class Webhook : Entity
{
    private readonly List<string> _events = [];

    /// <summary>FK to the owning organisation.</summary>
    public Guid OrganizationId { get; private init; }

    /// <summary>The URL CredVault POSTs delivery payloads to.</summary>
    public string Url { get; private set; } = string.Empty;

    /// <summary>FK into the encrypted-secrets table where the webhook signing secret is stored.</summary>
    public Guid SigningSecretReferenceId { get; private init; }

    /// <summary>Subscribed event names. Each must be a known value from <see cref="WebhookEventTypes"/>.</summary>
    public IReadOnlyList<string> Events => _events;

    /// <summary>Whether deliveries should be attempted.</summary>
    public bool IsActive { get; private set; }

    /// <summary>UTC instant the webhook was registered.</summary>
    public DateTime CreatedAtUtc { get; private init; }

    private Webhook() { }

    /// <summary>Creates a new active webhook.</summary>
    public static Webhook Create(
        Guid organizationId,
        string url,
        Guid signingSecretReferenceId,
        IEnumerable<string> events,
        IDateTimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(url);
        ArgumentNullException.ThrowIfNull(events);
        ArgumentNullException.ThrowIfNull(clock);
        if (organizationId == Guid.Empty)
            throw new DomainException("OrganizationId must not be empty.");
        if (signingSecretReferenceId == Guid.Empty)
            throw new DomainException("SigningSecretReferenceId must not be empty.");
        if (string.IsNullOrWhiteSpace(url))
            throw new DomainException("Webhook URL must not be empty.");
        if (!Uri.TryCreate(url, UriKind.Absolute, out var parsed) || (parsed.Scheme != Uri.UriSchemeHttp && parsed.Scheme != Uri.UriSchemeHttps))
            throw new DomainException("Webhook URL must be an absolute http(s) URL.");

        var eventList = events.ToList();
        if (eventList.Count == 0)
            throw new DomainException("Webhook must subscribe to at least one event.");
        foreach (var name in eventList)
        {
            if (!WebhookEventTypes.IsKnown(name))
                throw new DomainException($"Unknown webhook event '{name}'.");
        }

        var webhook = new Webhook
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Url = url,
            SigningSecretReferenceId = signingSecretReferenceId,
            IsActive = true,
            CreatedAtUtc = clock.UtcNow,
        };
        webhook._events.AddRange(eventList.Distinct(StringComparer.Ordinal));
        return webhook;
    }

    /// <summary>Replaces the URL.</summary>
    public void UpdateUrl(string newUrl)
    {
        if (string.IsNullOrWhiteSpace(newUrl))
            throw new DomainException("Webhook URL must not be empty.");
        if (!Uri.TryCreate(newUrl, UriKind.Absolute, out var parsed) || (parsed.Scheme != Uri.UriSchemeHttp && parsed.Scheme != Uri.UriSchemeHttps))
            throw new DomainException("Webhook URL must be an absolute http(s) URL.");
        Url = newUrl;
    }

    /// <summary>Replaces the subscribed event set.</summary>
    public void UpdateEvents(IEnumerable<string> newEvents)
    {
        ArgumentNullException.ThrowIfNull(newEvents);
        var list = newEvents.ToList();
        if (list.Count == 0)
            throw new DomainException("Webhook must subscribe to at least one event.");
        foreach (var name in list)
        {
            if (!WebhookEventTypes.IsKnown(name))
                throw new DomainException($"Unknown webhook event '{name}'.");
        }
        _events.Clear();
        _events.AddRange(list.Distinct(StringComparer.Ordinal));
    }

    /// <summary>Pauses deliveries.</summary>
    public void Deactivate() => IsActive = false;

    /// <summary>Resumes deliveries.</summary>
    public void Activate() => IsActive = true;
}
