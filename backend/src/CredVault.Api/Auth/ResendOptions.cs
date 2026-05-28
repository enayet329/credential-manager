namespace CredVault.Api.Auth;

/// <summary>Options for the Resend HTTP API (https://resend.com).</summary>
public sealed class ResendOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Resend";

    /// <summary>API key. Format: <c>re_…</c>. Leave blank to disable.</summary>
    public string ApiKey { get; init; } = string.Empty;

    /// <summary>
    /// From address. <c>onboarding@resend.dev</c> is Resend's default sandbox sender that
    /// works for accounts that haven't verified their own domain yet.
    /// </summary>
    public string FromAddress { get; init; } = "onboarding@resend.dev";

    /// <summary>Display name on the From header.</summary>
    public string FromName { get; init; } = "CredVault";
}
