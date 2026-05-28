using CredVault.Api.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace CredVault.Api.Auth;

/// <summary>
/// When an authorization policy with the <see cref="StepUpRequirement"/> fails, write a structured
/// 403 ProblemDetails with <c>code: "step_up_required"</c> so clients can prompt for MFA.
/// </summary>
public sealed class StepUpAwareResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _default = new();

    /// <inheritdoc/>
    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(authorizeResult);

        if (authorizeResult.Forbidden && policy.Requirements.OfType<StepUpRequirement>().Any())
        {
            var problem = ProblemDetailsHelpers.StepUpRequired();
            await problem.ExecuteAsync(context).ConfigureAwait(false);
            return;
        }

        await _default.HandleAsync(next, context, policy, authorizeResult).ConfigureAwait(false);
    }
}
