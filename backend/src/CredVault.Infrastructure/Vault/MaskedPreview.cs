using CredVault.Domain.Credentials;

namespace CredVault.Infrastructure.Vault;

/// <summary>Computes the ≤16-character preview shown in credential listings.</summary>
internal static class MaskedPreview
{
    /// <summary>
    /// Picks the first <see cref="CredentialField.IsSecret"/> field whose value is ≥ 16 chars and
    /// returns <c>"first4…last4"</c>. Falls back to the first non-empty field, or empty when there is
    /// nothing to show.
    /// </summary>
    public static string Compute(CredentialSchema schema, IReadOnlyDictionary<string, string> fields)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(fields);

        foreach (var field in schema.Fields)
        {
            if (!field.IsSecret) continue;
            if (!fields.TryGetValue(field.Key, out var value) || string.IsNullOrEmpty(value)) continue;
            if (value.Length < 16) continue;
            return $"{value[..4]}…{value[^4..]}";
        }

        foreach (var field in schema.Fields)
        {
            if (!fields.TryGetValue(field.Key, out var value) || string.IsNullOrEmpty(value)) continue;
            var trimmed = value.Length <= 16 ? value : value[..16];
            return trimmed;
        }

        return string.Empty;
    }
}
