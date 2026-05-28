using System.Text.RegularExpressions;

namespace CredVault.Domain.ValueObjects;

/// <summary>
/// A validated email address. Compared case-insensitively on the local + domain parts and stored
/// in lower-case to make uniqueness queries deterministic.
/// </summary>
public sealed partial record Email
{
    /// <summary>Maximum length accepted (RFC 5321 caps the path at 254 octets).</summary>
    public const int MaxLength = 254;

    [GeneratedRegex(@"^[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}$", RegexOptions.CultureInvariant)]
    private static partial Regex Pattern();

    /// <summary>The canonical lower-case email value.</summary>
    public string Value { get; }

    private Email(string value) => Value = value;

    /// <summary>Parses and validates an email. Throws <see cref="DomainException"/> on invalid input.</summary>
    /// <param name="raw">The input email address. Leading/trailing whitespace is trimmed; the result is lower-cased.</param>
    public static Email Create(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new DomainException("Email must not be empty.");

        var trimmed = raw.Trim();
        if (trimmed.Length > MaxLength)
            throw new DomainException($"Email must be at most {MaxLength} characters.");

        if (!Pattern().IsMatch(trimmed))
            throw new DomainException($"'{raw}' is not a valid email address.");

        return new Email(trimmed.ToLowerInvariant());
    }

    /// <inheritdoc/>
    public override string ToString() => Value;
}
