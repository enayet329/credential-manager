namespace CredVault.Domain.Enums;

/// <summary>Identifies the kind of principal recorded on an access-log row.</summary>
public enum ActorType
{
    /// <summary>A human user authenticated with their account.</summary>
    User = 0,

    /// <summary>A long-lived service token (CI/CD, scripts, the CLI).</summary>
    ServiceToken = 1,
}
