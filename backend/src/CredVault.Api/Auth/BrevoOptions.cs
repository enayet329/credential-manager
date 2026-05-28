namespace CredVault.Api.Auth;

/// <summary>Options for the Brevo HTTP API (faster + more reliable than SMTP relay).</summary>
public sealed class BrevoOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Brevo";

    /// <summary>API v3 key. Format: <c>xkeysib-...</c>. Leave blank to disable.</summary>
    public string ApiKey { get; init; } = string.Empty;

    /// <summary>From address. Must be a verified sender on your Brevo account.</summary>
    public string FromAddress { get; init; } = "no-reply@credvault.local";

    /// <summary>Display name on the From header.</summary>
    public string FromName { get; init; } = "CredVault";
}
