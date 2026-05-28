using System.Text;
using System.Text.Json;

namespace CredVault.Infrastructure.Vault;

/// <summary>
/// Serialises a dictionary into JSON with keys sorted in ordinal order. Used so an encrypted credential
/// payload is byte-stable regardless of insertion order — important because the AAD is derived from
/// the credential id, not the JSON, so we want the JSON to round-trip predictably.
/// </summary>
internal static class CanonicalJson
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static byte[] SerializeFields(IReadOnlyDictionary<string, string> fields)
    {
        ArgumentNullException.ThrowIfNull(fields);
        var ordered = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var (k, v) in fields)
            ordered[k] = v;

        using var ms = new MemoryStream();
        using (var writer = new Utf8JsonWriter(ms))
        {
            writer.WriteStartObject();
            foreach (var (k, v) in ordered)
                writer.WriteString(k, v);
            writer.WriteEndObject();
        }
        return ms.ToArray();
    }

    public static IReadOnlyDictionary<string, string> DeserializeFields(byte[] utf8Json)
    {
        ArgumentNullException.ThrowIfNull(utf8Json);
        var result = JsonSerializer.Deserialize<Dictionary<string, string>>(utf8Json, JsonOptions);
        return result is null
            ? new Dictionary<string, string>()
            : new Dictionary<string, string>(result, StringComparer.Ordinal);
    }

    public static Encoding Utf8 => Encoding.UTF8;
}
