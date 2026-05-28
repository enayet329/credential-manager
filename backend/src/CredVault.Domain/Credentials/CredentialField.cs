namespace CredVault.Domain.Credentials;

/// <summary>
/// One field inside a <see cref="CredentialSchema"/>. The UI uses these to render a form; the
/// application layer uses them to validate inbound JSON.
/// </summary>
/// <param name="Key">Stable machine key (snake_case), e.g. <c>"api_key"</c>.</param>
/// <param name="DisplayName">Human label shown in the UI.</param>
/// <param name="FieldType">Rendering / validation hint.</param>
/// <param name="IsRequired">Whether the field is required at credential-create time.</param>
/// <param name="IsSecret">Whether the value should be masked in logs and listings.</param>
/// <param name="Placeholder">Optional UI placeholder.</param>
/// <param name="ValidationRegex">Optional regex applied at the application layer.</param>
/// <param name="HelpText">Optional inline help shown next to the field.</param>
public sealed record CredentialField(
    string Key,
    string DisplayName,
    FieldType FieldType,
    bool IsRequired,
    bool IsSecret,
    string? Placeholder = null,
    string? ValidationRegex = null,
    string? HelpText = null);
