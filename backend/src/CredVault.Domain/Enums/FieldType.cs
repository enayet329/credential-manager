namespace CredVault.Domain.Enums;

/// <summary>UI/CLI hint for how a <see cref="CredVault.Domain.Credentials.CredentialField"/> should be rendered and validated.</summary>
public enum FieldType
{
    /// <summary>Plain single-line text.</summary>
    Text = 0,

    /// <summary>Sensitive single-line value — masked in UIs and never echoed to the CLI.</summary>
    Password = 1,

    /// <summary>URL — validated as a well-formed URI at the application layer.</summary>
    Url = 2,

    /// <summary>Multi-line text (e.g. a PEM blob).</summary>
    MultiLine = 3,
}
