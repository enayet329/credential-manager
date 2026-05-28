namespace CredVault.Infrastructure.Vault;

/// <summary>
/// Options for the short-lived in-process plaintext cache. <see cref="Enabled"/> defaults to <c>false</c>
/// so production is opt-in.
/// </summary>
public sealed class PlaintextCacheOptions
{
    /// <summary>Configuration section name to bind from.</summary>
    public const string SectionName = "Vault:PlaintextCache";

    /// <summary>Master switch. <c>false</c> = every retrieve re-decrypts.</summary>
    public bool Enabled { get; init; }

    /// <summary>Default TTL applied to cached entries.</summary>
    public TimeSpan Ttl { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>Hard ceiling — entries are capped to this regardless of <see cref="Ttl"/>.</summary>
    public TimeSpan MaxTtl { get; init; } = TimeSpan.FromSeconds(30);
}
