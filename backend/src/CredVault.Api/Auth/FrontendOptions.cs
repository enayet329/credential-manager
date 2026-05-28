namespace CredVault.Api.Auth;

/// <summary>
/// Where the user-facing web app lives. The API uses this to construct
/// share URLs and links inside transactional emails.
/// </summary>
public sealed class FrontendOptions
{
    /// <summary>Configuration section name to bind from.</summary>
    public const string SectionName = "Frontend";

    /// <summary>Absolute base URL, no trailing slash. Example: <c>https://app.credvault.io</c>.</summary>
    public string BaseUrl { get; init; } = "http://localhost:3000";

    /// <summary>Full URL of the login page.</summary>
    public string LoginUrl => $"{BaseUrl.TrimEnd('/')}/login";

    /// <summary>Full URL of a share-redemption page for the given token.</summary>
    public string ShareUrl(string token) => $"{BaseUrl.TrimEnd('/')}/share/{Uri.EscapeDataString(token)}";
}
