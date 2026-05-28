namespace CredVault.Domain.Organizations;

/// <summary>
/// Top-level tenancy boundary. Owns memberships, projects, and credential suppliers. Every other
/// entity in the domain is scoped to exactly one organisation.
/// </summary>
public sealed class Organization : Entity
{
    private readonly List<OrganizationMembership> _memberships = [];

    /// <summary>Display name shown in the UI. Free-form, 1–100 characters.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>URL-safe slug, unique globally. Used in CLI paths.</summary>
    public Slug Slug { get; private set; } = null!;

    /// <summary>UTC instant the organisation was created.</summary>
    public DateTime CreatedAtUtc { get; private init; }

    /// <summary>Whether the organisation is currently active. Inactive organisations are read-only.</summary>
    public bool IsActive { get; private set; }

    /// <summary>The membership rows for this organisation.</summary>
    public IReadOnlyList<OrganizationMembership> Memberships => _memberships;

    private Organization() { }

    /// <summary>Creates a new active organisation with the given name and slug.</summary>
    public static Organization Create(string name, Slug slug, IDateTimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(slug);
        ArgumentNullException.ThrowIfNull(clock);

        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Organization name must not be empty.");
        if (name.Length > 100)
            throw new DomainException("Organization name must be at most 100 characters.");

        return new Organization
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Slug = slug,
            CreatedAtUtc = clock.UtcNow,
            IsActive = true,
        };
    }

    /// <summary>Renames the organisation. Slug is immutable; create a new org if it must change.</summary>
    public void Rename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new DomainException("Organization name must not be empty.");
        if (newName.Length > 100)
            throw new DomainException("Organization name must be at most 100 characters.");
        Name = newName.Trim();
    }

    /// <summary>Adds a user to the organisation with the given role. Throws if the user is already a member.</summary>
    public OrganizationMembership AddMember(Guid userId, OrganizationRole role, IDateTimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        if (userId == Guid.Empty)
            throw new DomainException("UserId must not be empty.");

        if (_memberships.Any(m => m.UserId == userId))
            throw new DomainException("User is already a member of this organization.");

        var membership = OrganizationMembership.Create(Id, userId, role, clock.UtcNow);
        _memberships.Add(membership);
        return membership;
    }

    /// <summary>Removes a user from the organisation. No-op if the user is not a member.</summary>
    public void RemoveMember(Guid userId)
    {
        var membership = _memberships.FirstOrDefault(m => m.UserId == userId)
            ?? throw new DomainException("User is not a member of this organization.");
        _memberships.Remove(membership);
    }

    /// <summary>Changes a member's role. Throws if the user is not a member.</summary>
    public void ChangeMemberRole(Guid userId, OrganizationRole newRole)
    {
        var membership = _memberships.FirstOrDefault(m => m.UserId == userId)
            ?? throw new DomainException("User is not a member of this organization.");
        membership.ChangeRole(newRole);
    }

    /// <summary>Marks the organisation inactive. Inactive orgs reject mutations at the application layer.</summary>
    public void Deactivate() => IsActive = false;

    /// <summary>Re-activates a previously deactivated organisation.</summary>
    public void Activate() => IsActive = true;
}
