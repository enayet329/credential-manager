using Microsoft.AspNetCore.Authorization;

namespace CredVault.Api.Auth;

/// <summary>
/// Marker requirement for endpoints that need a recent MFA step-up. Satisfied when the request carries
/// a valid <c>X-Step-Up</c> header (a short-lived JWT with the <c>step_up=true</c> claim).
/// </summary>
public sealed class StepUpRequirement : IAuthorizationRequirement;
