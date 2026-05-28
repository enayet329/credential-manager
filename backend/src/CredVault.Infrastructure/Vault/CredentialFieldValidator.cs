using System.Text.RegularExpressions;
using CredVault.Domain.Credentials;

namespace CredVault.Infrastructure.Vault;

/// <summary>Validates inbound field dictionaries against a credential schema.</summary>
internal static class CredentialFieldValidator
{
    public static void Validate(CredentialSchema schema, IReadOnlyDictionary<string, string> fields)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(fields);

        var allowedKeys = new HashSet<string>(schema.Fields.Select(f => f.Key), StringComparer.Ordinal);
        foreach (var key in fields.Keys)
        {
            if (!allowedKeys.Contains(key))
                throw new DomainException($"Unknown field '{key}' for supplier '{schema.SupplierType}'.");
        }

        foreach (var field in schema.Fields)
        {
            fields.TryGetValue(field.Key, out var value);
            var present = !string.IsNullOrEmpty(value);

            if (field.IsRequired && !present)
                throw new DomainException($"Field '{field.Key}' is required.");

            if (present && field.ValidationRegex is { Length: > 0 } pattern)
            {
                if (!Regex.IsMatch(value!, pattern, RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(50)))
                    throw new DomainException($"Field '{field.Key}' does not match the required pattern.");
            }
        }
    }
}
