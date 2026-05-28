using Microsoft.Extensions.Logging;

namespace CredVault.Api.Auth;

/// <summary>
/// Dev/test implementation of <see cref="IEmailSender"/> that logs the message instead of
/// delivering it. Production deployments should replace this with an SMTP or SendGrid sender.
/// </summary>
public sealed class LoggingEmailSender : IEmailSender
{
    private static readonly Action<ILogger, string, string, string, Exception?> LogSent =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Information,
            new EventId(5001, nameof(LoggingEmailSender)),
            "[email] to={To} subject={Subject} body={Body}");

    private readonly ILogger<LoggingEmailSender> _logger;

    public LoggingEmailSender(ILogger<LoggingEmailSender> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(toEmail);
        ArgumentNullException.ThrowIfNull(subject);
        ArgumentNullException.ThrowIfNull(body);
        LogSent(_logger, toEmail, subject, body, null);
        return Task.CompletedTask;
    }
}
