using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CredVault.Infrastructure.Persistence.Conversions;

/// <summary>JSON-array round-trip for <c>List&lt;string&gt;</c> backing fields stored in a single column.</summary>
internal static class StringListJsonConverter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>Serialiser for a <c>List&lt;string&gt;</c> field.</summary>
    public static readonly ValueConverter<List<string>, string> Converter = new(
        v => JsonSerializer.Serialize(v, JsonOptions),
        v => JsonSerializer.Deserialize<List<string>>(v, JsonOptions) ?? new List<string>());

    /// <summary>Value comparer required because <c>List&lt;string&gt;</c> is a mutable reference type.</summary>
    public static readonly ValueComparer<List<string>> Comparer = new(
        (left, right) => (left == null && right == null) ||
                         (left != null && right != null && left.SequenceEqual(right, StringComparer.Ordinal)),
        list => list == null
            ? 0
            : list.Aggregate(0, (hash, item) => HashCode.Combine(hash, item)),
        list => list == null ? new List<string>() : new List<string>(list));
}
