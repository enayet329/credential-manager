using System.ComponentModel.DataAnnotations;

namespace CredVault.Api.Contracts;

/// <summary>Request body for <c>POST /auth/step-up</c>.</summary>
public sealed record StepUpRequest(
    [property: Required, MinLength(6), MaxLength(20)] string MfaCode);

/// <summary>Response with a short-lived step-up JWT.</summary>
public sealed record StepUpResponse(string StepUpToken, DateTime ExpiresAtUtc);
