using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace CredVault.Api.Auth;

/// <summary>
/// Real SMTP implementation of <see cref="IEmailSender"/>, backed by MailKit. Activated automatically
/// when <c>Smtp:Host</c> is configured; otherwise <see cref="LoggingEmailSender"/> is used.
/// </summary>
public sealed class SmtpEmailSender : IEmailSender
{
    private static readonly Action<ILogger, string, string, string, Exception?> LogSent =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Information,
            new EventId(5101, nameof(SmtpEmailSender)),
            "Sent email via SMTP host={Host} to={To} subject={Subject}");

    private static readonly Action<ILogger, string, Exception?> LogFailed =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(5102, nameof(SmtpEmailSender)),
            "SMTP send failed for {To}");

    private readonly SmtpOptions _options;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<SmtpOptions> options, ILogger<SmtpEmailSender> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(toEmail);
        ArgumentNullException.ThrowIfNull(subject);
        ArgumentNullException.ThrowIfNull(body);

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = body };

        using var client = new SmtpClient();
        // Disable CRL/OCSP revocation checks. Chain validation against the OS trust store still
        // applies — this just stops MailKit from hard-failing when a CRL endpoint is unreachable,
        // which is common on dev networks and behind some corporate proxies.
        client.CheckCertificateRevocation = false;
        try
        {
            var secureOptions = _options.UseStartTls
                ? SecureSocketOptions.StartTls
                : SecureSocketOptions.SslOnConnect;

            await client.ConnectAsync(_options.Host, _options.Port, secureOptions, cancellationToken).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(_options.Username))
                await client.AuthenticateAsync(_options.Username, _options.Password, cancellationToken).ConfigureAwait(false);

            await client.SendAsync(message, cancellationToken).ConfigureAwait(false);
            LogSent(_logger, _options.Host, toEmail, subject, null);
        }
        catch (Exception ex)
        {
            LogFailed(_logger, toEmail, ex);
            throw;
        }
        finally
        {
            await client.DisconnectAsync(quit: true, cancellationToken).ConfigureAwait(false);
        }
    }
}
