namespace CredVault.Domain.Organizations;

/// <summary>
/// Join row between a <see cref="User"/> and an <see cref="Organization"/>. Carries the user's role
/// within that organisation.
/// </summary>
public sealed class OrganizationMembership : Entity
{
    /// <summary>FK to the organization.</summary>
    public Guid OrganizationId { get; private init; }

    /// <summary>FK to the user.</summary>
    public Guid UserId { get; private init; }

    /// <summary>The user's role inside the organisation.</summary>
    public OrganizationRole Role { get; private set; }

    /// <summary>UTC instant the user joined.</summary>
    public DateTime JoinedAtUtc { get; private init; }

    private OrganizationMembership() { }

    internal static OrganizationMembership Create(
        Guid organizationId,
        Guid userId,
        OrganizationRole role,
        DateTime joinedAtUtc) =>
        new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            UserId = userId,
            Role = role,
            JoinedAtUtc = joinedAtUtc,
        };

    /// <summary>Changes the role recorded on this membership row.</summary>
    public void ChangeRole(OrganizationRole newRole) => Role = newRole;
}
