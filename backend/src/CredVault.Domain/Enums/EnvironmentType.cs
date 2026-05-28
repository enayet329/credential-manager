namespace CredVault.Domain.Enums;

/// <summary>Classification of an environment for UX surfacing (badge colour, warnings, etc.).</summary>
public enum EnvironmentType
{
    /// <summary>Engineer laptops, ephemeral CI, low-impact testing.</summary>
    Development = 0,

    /// <summary>User-acceptance testing — typically shared with QA / product.</summary>
    Uat = 1,

    /// <summary>Pre-production staging — should resemble production closely.</summary>
    Staging = 2,

    /// <summary>Production — customer-facing. Triggers extra confirmation in the UI and CLI.</summary>
    Production = 3,

    /// <summary>An organisation-defined environment that doesn't fit the standard ladder.</summary>
    Custom = 4,
}
