using System.ComponentModel.DataAnnotations;

namespace CredVault.Api.Auth;

/// <summary>Options binding for JWT signing/validation.</summary>
public sealed class JwtOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Jwt";

    /// <summary>HS256 signing key. Must be ≥ 32 bytes after UTF8 encoding.</summary>
    [Required, MinLength(32)]
    public string Secret { get; init; } = string.Empty;

    /// <summary>JWT issuer claim. Set to your deployed hostname.</summary>
    [Required]
    public string Issuer { get; init; } = string.Empty;

    /// <summary>JWT audience claim.</summary>
    [Required]
    public string Audience { get; init; } = string.Empty;
}
