namespace CredVault.Domain.Webhooks;

/// <summary>
/// The set of event names a webhook can subscribe to. Constants are the wire-format strings sent in
/// the delivery payload's <c>event</c> field.
/// </summary>
public static class WebhookEventTypes
{
    /// <summary>A credential was created.</summary>
    public const string CredentialCreated = "credential.created";

    /// <summary>A credential's secret material was rotated.</summary>
    public const string CredentialRotated = "credential.rotated";

    /// <summary>A credential was decrypted. Opt-in (high volume).</summary>
    public const string CredentialAccessed = "credential.accessed";

    /// <summary>A credential is approaching its expiry date (30/7/1 days).</summary>
    public const string CredentialExpiring = "credential.expiring";

    /// <summary>A credential was administratively revoked.</summary>
    public const string CredentialRevoked = "credential.revoked";

    /// <summary>A service token was minted.</summary>
    public const string ServiceTokenCreated = "service_token.created";

    /// <summary>A service token was used at the auth layer for the first time.</summary>
    public const string ServiceTokenFirstUse = "service_token.first_use";

    /// <summary>A user was added to the organisation.</summary>
    public const string MemberAdded = "member.added";

    /// <summary>An existing member's role was changed.</summary>
    public const string MemberRoleChanged = "member.role_changed";

    /// <summary>Every supported event name in declaration order.</summary>
    public static IReadOnlyCollection<string> All { get; } =
    [
        CredentialCreated,
        CredentialRotated,
        CredentialAccessed,
        CredentialExpiring,
        CredentialRevoked,
        ServiceTokenCreated,
        ServiceTokenFirstUse,
        MemberAdded,
        MemberRoleChanged,
    ];

    /// <summary>Whether <paramref name="eventName"/> is a known event type.</summary>
    public static bool IsKnown(string eventName) => All.Contains(eventName);
}
