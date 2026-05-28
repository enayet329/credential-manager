namespace CredVault.Api.Auth;

/// <summary>
/// Outbound transactional email. Swap <see cref="LoggingEmailSender"/> for a real SMTP/SendGrid
/// implementation in production — the interface is intentionally minimal so that's a drop-in change.
/// </summary>
public interface IEmailSender
{
    /// <summary>Sends an email. Returns when the provider has accepted it (or for the dev stub, when it's been logged).</summary>
    Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default);
}
