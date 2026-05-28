namespace CredVault.Infrastructure.Vault;

/// <summary>Rate-limit configuration for credential decryption.</summary>
public sealed class VaultRateLimitOptions
{
    /// <summary>Configuration section name to bind from.</summary>
    public const string SectionName = "Vault:RateLimit";

    /// <summary>Decrypts permitted per credential per minute.</summary>
    public int PerCredentialPerMinute { get; init; } = 60;
}
