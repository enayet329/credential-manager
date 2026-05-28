namespace CredVault.Domain.Projects;

/// <summary>
/// A logical grouping of environments and credentials inside an organisation. Maps roughly to one
/// product / repository / deployable unit.
/// </summary>
public sealed class Project : Entity
{
    /// <summary>FK to the owning organisation. Set at creation and never changes.</summary>
    public Guid OrganizationId { get; private init; }

    /// <summary>Display name shown in the UI.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>URL-safe slug, unique within the organisation.</summary>
    public Slug Slug { get; private set; } = null!;

    /// <summary>Optional free-form description.</summary>
    public string? Description { get; private set; }

    /// <summary>UTC instant the project was created.</summary>
    public DateTime CreatedAtUtc { get; private init; }

    private Project() { }

    /// <summary>Creates a new project owned by the given organisation.</summary>
    public static Project Create(
        Guid organizationId,
        string name,
        Slug slug,
        string? description,
        IDateTimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(slug);
        ArgumentNullException.ThrowIfNull(clock);
        if (organizationId == Guid.Empty)
            throw new DomainException("OrganizationId must not be empty.");
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Project name must not be empty.");
        if (name.Length > 100)
            throw new DomainException("Project name must be at most 100 characters.");
        if (description is not null && description.Length > 500)
            throw new DomainException("Project description must be at most 500 characters.");

        return new Project
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Name = name.Trim(),
            Slug = slug,
            Description = description?.Trim(),
            CreatedAtUtc = clock.UtcNow,
        };
    }

    /// <summary>Renames the project. Slug remains immutable.</summary>
    public void Rename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new DomainException("Project name must not be empty.");
        if (newName.Length > 100)
            throw new DomainException("Project name must be at most 100 characters.");
        Name = newName.Trim();
    }

    /// <summary>Updates the project description.</summary>
    public void UpdateDescription(string? newDescription)
    {
        if (newDescription is not null && newDescription.Length > 500)
            throw new DomainException("Project description must be at most 500 characters.");
        Description = newDescription?.Trim();
    }
}
