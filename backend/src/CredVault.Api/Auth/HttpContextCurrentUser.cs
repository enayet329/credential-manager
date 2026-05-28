using System.Security.Claims;
using CredVault.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace CredVault.Api.Auth;

/// <summary>Reads the current request's claims and headers to derive actor info.</summary>
public sealed class HttpContextCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public HttpContextCurrentUser(IHttpContextAccessor accessor)
    {
        ArgumentNullException.ThrowIfNull(accessor);
        _accessor = accessor;
    }

    private HttpContext? Ctx => _accessor.HttpContext;

    /// <inheritdoc/>
    public bool IsAuthenticated => Ctx?.User.Identity?.IsAuthenticated == true;

    /// <inheritdoc/>
    public Guid ActorId
    {
        get
        {
            var sub = Ctx?.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? Ctx?.User.FindFirstValue("sub");
            return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
        }
    }

    /// <inheritdoc/>
    public ActorType ActorType
    {
        get
        {
            var typed = Ctx?.User.FindFirstValue("actor_type");
            return string.Equals(typed, "service_token", StringComparison.OrdinalIgnoreCase)
                ? ActorType.ServiceToken
                : ActorType.User;
        }
    }

    /// <inheritdoc/>
    public bool HasPermission(string permission)
    {
        ArgumentNullException.ThrowIfNull(permission);
        if (Ctx is null) return false;
        foreach (var claim in Ctx.User.FindAll(AuthConstants.PermissionsClaim))
        {
            if (string.Equals(claim.Value, permission, StringComparison.Ordinal))
                return true;
        }
        return false;
    }

    /// <inheritdoc/>
    public string IpAddress => Ctx?.Connection.RemoteIpAddress?.ToString() ?? string.Empty;

    /// <inheritdoc/>
    public string UserAgent => Ctx?.Request.Headers.UserAgent.ToString() ?? string.Empty;
}
