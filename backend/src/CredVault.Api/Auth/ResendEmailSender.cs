using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Resend;

namespace CredVault.Api.Auth;

/// <summary>
/// Sends email via the Resend HTTP API. Used when <c>Resend:ApiKey</c> is configured —
/// takes priority over Brevo and SMTP. Resend's <c>onboarding@resend.dev</c> default sender
/// works without domain verification, which is the easiest path from zero to working email.
/// </summary>
public sealed class ResendEmailSender : IEmailSender
{
    private static readonly Action<ILogger, string, string, string?, Exception?> LogSent =
        LoggerMessage.Define<string, string, string?>(
            LogLevel.Information,
            new EventId(5301, nameof(ResendEmailSender)),
            "Sent email via Resend to={To} subject={Subject} resendId={ResendId}");

    private static readonly Action<ILogger, string, string, Exception?> LogFailed =
        LoggerMessage.Define<string, string>(
            LogLevel.Error,
            new EventId(5302, nameof(ResendEmailSender)),
            "Resend send failed for {To}: {Message}");

    private readonly IResend _resend;
    private readonly ResendOptions _options;
    private readonly ILogger<ResendEmailSender> _logger;

    public ResendEmailSender(IResend resend, IOptions<ResendOptions> options, ILogger<ResendEmailSender> logger)
    {
        ArgumentNullException.ThrowIfNull(resend);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        _resend = resend;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(toEmail);
        ArgumentNullException.ThrowIfNull(subject);
        ArgumentNullException.ThrowIfNull(body);

        var message = new EmailMessage
        {
            From = $"{_options.FromName} <{_options.FromAddress}>",
            Subject = subject,
            TextBody = body,
            HtmlBody = $"<pre style=\"font-family:ui-monospace,monospace;white-space:pre-wrap\">{System.Net.WebUtility.HtmlEncode(body)}</pre>",
        };
        message.To.Add(toEmail);

        try
        {
            var response = await _resend.EmailSendAsync(message, cancellationToken).ConfigureAwait(false);
            LogSent(_logger, toEmail, subject, response?.Content.ToString(), null);
        }
        catch (Exception ex)
        {
            LogFailed(_logger, toEmail, ex.Message, ex);
            throw;
        }
    }
}
