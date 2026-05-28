using System.Runtime.CompilerServices;
using CredVault.Domain.Credentials;

namespace CredVault.Infrastructure.Vault;

/// <summary>
/// Zero-overhead accessors that let Infrastructure satisfy domain invariants the public API can't
/// express without touching the Domain. Specifically: credential and credential-note ids participate
/// in their encryption AAD, but the ids are generated inside the domain factories. We pre-generate
/// each id here and overwrite the entity's id once the envelope (already bound to the chosen id) is
/// in place.
/// </summary>
public static class DomainAccessors
{
    // The setter for Entity.Id is declared on the abstract base class, not on derived types, so the
    // UnsafeAccessor must target Entity to find the method.
    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_Id")]
    private static extern void SetEntityId(Entity target, Guid value);

    /// <summary>Overrides a credential's <see cref="Entity.Id"/> after construction.</summary>
    public static void SetCredentialId(Credential target, Guid value)
    {
        ArgumentNullException.ThrowIfNull(target);
        SetEntityId(target, value);
    }

    /// <summary>Overrides a credential note's <see cref="Entity.Id"/> after construction.</summary>
    public static void SetCredentialNoteId(CredentialNote target, Guid value)
    {
        ArgumentNullException.ThrowIfNull(target);
        SetEntityId(target, value);
    }
}
