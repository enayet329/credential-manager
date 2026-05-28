using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CredVault.Api.Auth;

/// <summary>
/// Sends transactional email via the Brevo (formerly Sendinblue) HTTP API. Used when
/// <c>Brevo:ApiKey</c> is configured — takes priority over <see cref="SmtpEmailSender"/>.
/// </summary>
public sealed class BrevoApiEmailSender : IEmailSender
{
    private const string Endpoint = "https://api.brevo.com/v3/smtp/email";

    private static readonly Action<ILogger, string, string, Exception?> LogSent =
        LoggerMessage.Define<string, string>(
            LogLevel.Information,
            new EventId(5201, nameof(BrevoApiEmailSender)),
            "Sent email via Brevo API to={To} subject={Subject}");

    private static readonly Action<ILogger, string, string, Exception?> LogFailed =
        LoggerMessage.Define<string, string>(
            LogLevel.Error,
            new EventId(5202, nameof(BrevoApiEmailSender)),
            "Brevo API send failed for {To}. Response: {Response}");

    private readonly BrevoOptions _options;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<BrevoApiEmailSender> _logger;

    public BrevoApiEmailSender(
        IOptions<BrevoOptions> options,
        IHttpClientFactory httpFactory,
        ILogger<BrevoApiEmailSender> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(httpFactory);
        ArgumentNullException.ThrowIfNull(logger);
        _options = options.Value;
        _httpFactory = httpFactory;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(toEmail);
        ArgumentNullException.ThrowIfNull(subject);
        ArgumentNullException.ThrowIfNull(body);

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new InvalidOperationException("Brevo:ApiKey is not configured.");

        var payload = new
        {
            sender = new { name = _options.FromName, email = _options.FromAddress },
            to = new[] { new { email = toEmail } },
            subject,
            textContent = body,
            // Same content rendered as basic HTML; recipients with HTML support get nicer formatting.
            htmlContent = $"<pre style=\"font-family:ui-monospace,monospace;white-space:pre-wrap\">{System.Net.WebUtility.HtmlEncode(body)}</pre>",
        };

        using var client = _httpFactory.CreateClient(nameof(BrevoApiEmailSender));
        using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint)
        {
            Content = JsonContent.Create(payload),
        };
        request.Headers.Add("api-key", _options.ApiKey);
        request.Headers.Add("accept", "application/json");

        using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            LogFailed(_logger, toEmail, content, null);
            throw new InvalidOperationException(
                $"Brevo returned {(int)response.StatusCode} {response.StatusCode}: {content}");
        }

        LogSent(_logger, toEmail, subject, null);
    }
}
