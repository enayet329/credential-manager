using CredVault.Domain.Enums;

namespace CredVault.Api.Auth;

/// <summary>Resolves the authenticated principal of the current HTTP request.</summary>
public interface ICurrentUser
{
    /// <summary>Whether a principal is attached (authenticated).</summary>
    bool IsAuthenticated { get; }

    /// <summary>Identity of the authenticated principal (user id or service-token id).</summary>
    Guid ActorId { get; }

    /// <summary>User vs service-token.</summary>
    ActorType ActorType { get; }

    /// <summary>Whether the user holds the supplied permission.</summary>
    bool HasPermission(string permission);

    /// <summary>Best-effort source IP for audit attribution. Empty when not known.</summary>
    string IpAddress { get; }

    /// <summary>Best-effort user-agent for audit attribution. Empty when not known.</summary>
    string UserAgent { get; }
}
