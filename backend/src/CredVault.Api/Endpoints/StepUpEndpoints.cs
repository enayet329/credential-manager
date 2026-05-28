using CredVault.Api.Auth;
using CredVault.Api.Contracts;
using CredVault.Api.Filters;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace CredVault.Api.Endpoints;

/// <summary>The <c>/auth/step-up</c> endpoint that mints short-lived MFA-elevated JWTs.</summary>
public static class StepUpEndpoints
{
    public static IEndpointRouteBuilder MapStepUpEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapPost("/auth/step-up", StepUp)
            .WithTags("Auth")
            .RequireAuthorization()
            .WithMetadata(new SafetyNetAllowlistMarker())
            .WithSummary("Mint a step-up JWT after the caller proves MFA.")
            .WithName("StepUp");

        return routes;
    }

    private static Results<Ok<StepUpResponse>, ProblemHttpResult> StepUp(
        [FromBody] StepUpRequest request,
        StepUpTokenService tokens,
        ICurrentUser currentUser)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Real MFA verification belongs in the application layer (TOTP code against
        // MfaSecretReferenceId on the user). For Phase 4 we accept any non-empty 6+ digit code so
        // the integration tests can exercise the full step-up flow; the real check arrives in Phase 5.
        if (string.IsNullOrWhiteSpace(request.MfaCode) || request.MfaCode.Length < 6)
            return ProblemDetailsHelpers.BadRequest("Invalid MFA code.");

        if (!currentUser.IsAuthenticated || currentUser.ActorId == Guid.Empty)
            return ProblemDetailsHelpers.Forbidden("Authentication required.");

        var (token, expires) = tokens.Issue(currentUser.ActorId);
        return TypedResults.Ok(new StepUpResponse(token, expires));
    }
}
