using System.Text.RegularExpressions;

namespace CredVault.Domain.ValueObjects;

/// <summary>
/// A URL-safe identifier matching <c>^[a-z0-9-]{3,50}$</c>. Used for organisation, project,
/// environment, supplier, and credential identifiers in CLI paths.
/// </summary>
public sealed partial record Slug
{
    /// <summary>Minimum slug length (inclusive).</summary>
    public const int MinLength = 3;

    /// <summary>Maximum slug length (inclusive).</summary>
    public const int MaxLength = 50;

    [GeneratedRegex("^[a-z0-9-]{3,50}$", RegexOptions.CultureInvariant)]
    private static partial Regex Pattern();

    /// <summary>The validated slug value.</summary>
    public string Value { get; }

    private Slug(string value) => Value = value;

    /// <summary>Parses and validates a slug. Throws <see cref="DomainException"/> on invalid input.</summary>
    public static Slug Create(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new DomainException("Slug must not be empty.");

        if (!Pattern().IsMatch(raw))
            throw new DomainException(
                $"Slug '{raw}' is invalid. Slugs must be {MinLength}-{MaxLength} characters of [a-z0-9-].");

        return new Slug(raw);
    }

    /// <inheritdoc/>
    public override string ToString() => Value;

    /// <summary>Implicit conversion to the underlying string for convenience.</summary>
    public static implicit operator string(Slug slug) => slug.Value;
}
