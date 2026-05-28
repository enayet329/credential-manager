using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;

namespace CredVault.Api.Auth;

/// <summary>
/// Validates the <c>X-Step-Up</c> JWT against the same signing key/issuer/audience as the main token
/// and confirms it carries <c>step_up=true</c> and hasn't expired.
/// </summary>
public sealed class StepUpAuthorizationHandler : AuthorizationHandler<StepUpRequirement>
{
    private readonly IHttpContextAccessor _accessor;
    private readonly JwtOptions _options;

    public StepUpAuthorizationHandler(IHttpContextAccessor accessor, IOptions<JwtOptions> options)
    {
        ArgumentNullException.ThrowIfNull(accessor);
        ArgumentNullException.ThrowIfNull(options);
        _accessor = accessor;
        _options = options.Value;
    }

    /// <inheritdoc/>
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, StepUpRequirement requirement)
    {
        var http = _accessor.HttpContext;
        if (http is null)
            return Task.CompletedTask;

        var header = http.Request.Headers[AuthConstants.StepUpHeader].ToString();
        if (string.IsNullOrWhiteSpace(header))
            return Task.CompletedTask;

        var handler = new JwtSecurityTokenHandler();
        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _options.Issuer,
            ValidateAudience = true,
            ValidAudience = _options.Audience,
            ValidateLifetime = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret)),
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.Zero,
        };

        try
        {
            var principal = handler.ValidateToken(header, parameters, out _);
            var stepUp = principal.FindFirstValue(AuthConstants.StepUpClaim);
            if (string.Equals(stepUp, "true", StringComparison.OrdinalIgnoreCase))
                context.Succeed(requirement);
        }
        catch (SecurityTokenException)
        {
            // not satisfied; downstream returns 403
        }

        return Task.CompletedTask;
    }
}
