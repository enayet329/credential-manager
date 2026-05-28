using System.ComponentModel.DataAnnotations;

namespace CredVault.Api.Auth;

/// <summary>Options binding for the <c>Smtp</c> configuration section.</summary>
public sealed class SmtpOptions
{
    /// <summary>Configuration section name to bind from.</summary>
    public const string SectionName = "Smtp";

    /// <summary>SMTP host. Leave blank to disable SMTP and fall back to the logging sender.</summary>
    public string Host { get; init; } = string.Empty;

    /// <summary>SMTP port. 587 = STARTTLS, 465 = implicit TLS.</summary>
    [Range(1, 65535)]
    public int Port { get; init; } = 587;

    /// <summary>Login. For Brevo this looks like <c>acc...@smtp-brevo.com</c>.</summary>
    public string Username { get; init; } = string.Empty;

    /// <summary>SMTP key / password. Treat as sensitive.</summary>
    public string Password { get; init; } = string.Empty;

    /// <summary>From address shown to recipients. Must be a verified sender on your SMTP account.</summary>
    public string FromAddress { get; init; } = "no-reply@credvault.local";

    /// <summary>Display name on the From header.</summary>
    public string FromName { get; init; } = "CredVault";

    /// <summary>Whether to use STARTTLS (true) or implicit TLS (false). 587 → true, 465 → false.</summary>
    public bool UseStartTls { get; init; } = true;
}
