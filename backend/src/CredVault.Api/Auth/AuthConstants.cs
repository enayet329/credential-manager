namespace CredVault.Api.Auth;

/// <summary>Shared claim names, header names, and policy ids used by the API auth layer.</summary>
public static class AuthConstants
{
    /// <summary>The custom header that carries the short-lived step-up JWT.</summary>
    public const string StepUpHeader = "X-Step-Up";

    /// <summary>Claim added to user JWTs once per permission they hold.</summary>
    public const string PermissionsClaim = "perms";

    /// <summary>Claim added to the step-up JWT to mark it.</summary>
    public const string StepUpClaim = "step_up";

    /// <summary>Authorization policy id for the step-up requirement.</summary>
    public const string StepUpPolicy = "step-up";

    /// <summary>Time-to-live for issued step-up tokens.</summary>
    public static readonly TimeSpan StepUpLifetime = TimeSpan.FromMinutes(5);
}
