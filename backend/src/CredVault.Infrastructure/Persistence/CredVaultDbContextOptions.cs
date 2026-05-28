namespace CredVault.Infrastructure.Persistence;

/// <summary>
/// Per-environment toggles for the <see cref="CredVaultDbContext"/>. Bound from the <c>Database</c>
/// configuration section.
/// </summary>
public sealed class CredVaultDbContextOptions
{
    /// <summary>Configuration section name to bind from.</summary>
    public const string SectionName = "Database";

    /// <summary>
    /// Whether the underlying SQL Server supports the native <c>json</c> column type. <c>false</c> on
    /// SQL Server 2022 and earlier — the configurations fall back to <c>nvarchar(max)</c>.
    /// </summary>
    public bool UseNativeJsonColumnType { get; init; }

    /// <summary>The column type string used for JSON-backed columns.</summary>
    public string JsonColumnType => UseNativeJsonColumnType ? "json" : "nvarchar(max)";
}
