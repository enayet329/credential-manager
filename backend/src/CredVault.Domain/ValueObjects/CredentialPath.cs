namespace CredVault.Domain.ValueObjects;

/// <summary>
/// A four-segment credential locator of the form
/// <c>{project}/{environment}/{supplier}/{credential}</c> — used by the CLI to identify a credential
/// without exposing surrogate IDs.
/// </summary>
public sealed record CredentialPath
{
    /// <summary>The project slug segment.</summary>
    public Slug Project { get; }

    /// <summary>The environment slug segment (e.g. <c>dev</c>, <c>staging</c>, <c>prod</c>).</summary>
    public Slug Environment { get; }

    /// <summary>The credential-supplier slug segment.</summary>
    public Slug Supplier { get; }

    /// <summary>The credential slug segment.</summary>
    public Slug Credential { get; }

    private CredentialPath(Slug project, Slug environment, Slug supplier, Slug credential)
    {
        Project = project;
        Environment = environment;
        Supplier = supplier;
        Credential = credential;
    }

    /// <summary>Constructs a path from four already-validated slugs.</summary>
    public static CredentialPath Create(Slug project, Slug environment, Slug supplier, Slug credential) =>
        new(project, environment, supplier, credential);

    /// <summary>
    /// Parses a string of the form <c>project/env/supplier/credential</c>. Throws <see cref="DomainException"/>
    /// if the structure is wrong or any segment fails <see cref="Slug"/> validation.
    /// </summary>
    public static CredentialPath Parse(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new DomainException("Credential path must not be empty.");

        var segments = raw.Trim().Split('/');
        if (segments.Length != 4)
            throw new DomainException(
                $"Credential path '{raw}' must have exactly four segments separated by '/'.");

        return new CredentialPath(
            Slug.Create(segments[0]),
            Slug.Create(segments[1]),
            Slug.Create(segments[2]),
            Slug.Create(segments[3]));
    }

    /// <inheritdoc/>
    public override string ToString() => $"{Project}/{Environment}/{Supplier}/{Credential}";
}
