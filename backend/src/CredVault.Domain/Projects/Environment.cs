namespace CredVault.Domain.Projects;

/// <summary>
/// A deployment target inside a <see cref="Project"/> (e.g. <c>dev</c>, <c>staging</c>, <c>prod</c>).
/// Credentials are scoped to one (project, environment, supplier) triple.
/// </summary>
public sealed class Environment : Entity
{
    /// <summary>FK to the owning project. Immutable after creation.</summary>
    public Guid ProjectId { get; private init; }

    /// <summary>Display name shown in the UI.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>URL-safe slug, unique within the project.</summary>
    public Slug Slug { get; private set; } = null!;

    /// <summary>Classification of this environment (used for UI badges and prod-safety prompts).</summary>
    public EnvironmentType Type { get; private set; }

    /// <summary>UTC instant the environment was created.</summary>
    public DateTime CreatedAtUtc { get; private init; }

    private Environment() { }

    /// <summary>Creates a new environment inside the given project.</summary>
    public static Environment Create(
        Guid projectId,
        string name,
        Slug slug,
        EnvironmentType type,
        IDateTimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(slug);
        ArgumentNullException.ThrowIfNull(clock);
        if (projectId == Guid.Empty)
            throw new DomainException("ProjectId must not be empty.");
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Environment name must not be empty.");
        if (name.Length > 100)
            throw new DomainException("Environment name must be at most 100 characters.");

        return new Environment
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Name = name.Trim(),
            Slug = slug,
            Type = type,
            CreatedAtUtc = clock.UtcNow,
        };
    }

    /// <summary>Renames the environment. Slug and type remain immutable.</summary>
    public void Rename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new DomainException("Environment name must not be empty.");
        if (newName.Length > 100)
            throw new DomainException("Environment name must be at most 100 characters.");
        Name = newName.Trim();
    }
}
