namespace CredVault.Domain.Enums;

/// <summary>Whether an access attempt resolved to a successful decrypt or was rejected before it.</summary>
public enum AccessOutcome
{
    /// <summary>Caller was authorised; secret was returned.</summary>
    Success = 0,

    /// <summary>Caller was rejected (scope mismatch, revoked token, IP not allowlisted, etc.).</summary>
    Denied = 1,
}
