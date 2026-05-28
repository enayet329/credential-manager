namespace CredVault.Infrastructure.Cryptography;

/// <summary>Options binding for the <c>MasterKey</c> configuration section.</summary>
public sealed class MasterKeyOptions
{
    /// <summary>Configuration section name to bind from.</summary>
    public const string SectionName = "MasterKey";

    /// <summary>Registered KEK versions. At least one must be present.</summary>
    public List<MasterKeyVersion> Versions { get; init; } = [];
}

/// <summary>A single KEK entry.</summary>
public sealed class MasterKeyVersion
{
    /// <summary>Version number. Must be strictly increasing across rotations.</summary>
    public int Version { get; init; }

    /// <summary>Base64-encoded 32-byte key.</summary>
    public string Base64Key { get; init; } = string.Empty;
}
