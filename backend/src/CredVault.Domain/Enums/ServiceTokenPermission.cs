using System.Diagnostics.CodeAnalysis;

namespace CredVault.Domain.Enums;

/// <summary>Permission level granted by a single <c>ServiceTokenScope</c>.</summary>
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix",
    Justification = "Domain models a permission grant; 'Permission' is the natural name.")]
public enum ServiceTokenPermission
{
    /// <summary>Decrypt and read credentials matching the scope.</summary>
    Read = 0,

    /// <summary>Create, rotate, and revoke credentials matching the scope (implies <see cref="Read"/>).</summary>
    Write = 1,
}
