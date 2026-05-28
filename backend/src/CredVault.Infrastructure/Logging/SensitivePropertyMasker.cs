using Serilog.Core;
using Serilog.Events;

namespace CredVault.Infrastructure.Logging;

/// <summary>
/// Serilog enricher that walks every log event and overwrites the value of any property whose name
/// matches a sensitive substring (case-insensitive). Applied as a chain: the destructurer first turns
/// objects into property bags; this enricher then sanitises them.
/// </summary>
public sealed class SensitivePropertyMasker : ILogEventEnricher
{
    /// <summary>The placeholder value substituted for any matched property.</summary>
    public const string MaskedValue = "***REDACTED***";

    /// <summary>Substrings (case-insensitive) that mark a property name as sensitive.</summary>
    public static readonly IReadOnlyList<string> SensitiveSubstrings =
    [
        "password",
        "apikey",
        "api_key",
        "secret",
        "secret_key",
        "token",
        "authorization",
        "access_key",
        "master_key",
        "kek",
        "dek",
        "credential",
        "private_key",
        "webhook_secret",
    ];

    /// <inheritdoc/>
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        ArgumentNullException.ThrowIfNull(logEvent);
        ArgumentNullException.ThrowIfNull(propertyFactory);

        foreach (var name in logEvent.Properties.Keys.ToList())
        {
            var sanitised = Sanitise(name, logEvent.Properties[name]);
            if (sanitised is not null)
                logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(name, sanitised, destructureObjects: false));
        }
    }

    /// <summary>
    /// Returns <c>true</c> if any sensitive substring is in <paramref name="propertyName"/>. Both the
    /// property name and the substrings are normalised by stripping underscores, so
    /// <c>PrivateKey</c>, <c>private_key</c>, and <c>privatekey</c> all match.
    /// </summary>
    public static bool IsSensitive(string propertyName)
    {
        ArgumentNullException.ThrowIfNull(propertyName);
        var normalised = propertyName.Replace("_", string.Empty, StringComparison.Ordinal);
        foreach (var needle in SensitiveSubstrings)
        {
            var normalisedNeedle = needle.Replace("_", string.Empty, StringComparison.Ordinal);
            if (normalised.Contains(normalisedNeedle, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private static object? Sanitise(string name, LogEventPropertyValue value)
    {
        if (IsSensitive(name))
            return MaskedValue;

        switch (value)
        {
            case StructureValue structure:
                {
                    var rewritten = new List<LogEventProperty>(structure.Properties.Count);
                    var changed = false;
                    foreach (var prop in structure.Properties)
                    {
                        var inner = Sanitise(prop.Name, prop.Value);
                        if (inner is not null)
                        {
                            rewritten.Add(new LogEventProperty(prop.Name, new ScalarValue(inner)));
                            changed = true;
                        }
                        else
                        {
                            rewritten.Add(prop);
                        }
                    }
                    return changed ? new StructureValue(rewritten, structure.TypeTag) : null;
                }
            case DictionaryValue dict:
                {
                    var rewritten = new Dictionary<ScalarValue, LogEventPropertyValue>();
                    var changed = false;
                    foreach (var pair in dict.Elements)
                    {
                        var keyText = pair.Key.Value?.ToString() ?? string.Empty;
                        var inner = Sanitise(keyText, pair.Value);
                        if (inner is not null)
                        {
                            rewritten[pair.Key] = new ScalarValue(inner);
                            changed = true;
                        }
                        else
                        {
                            rewritten[pair.Key] = pair.Value;
                        }
                    }
                    return changed ? new DictionaryValue(rewritten) : null;
                }
            case SequenceValue sequence:
                {
                    var rewritten = new List<LogEventPropertyValue>(sequence.Elements.Count);
                    var changed = false;
                    foreach (var item in sequence.Elements)
                    {
                        var inner = Sanitise(name, item);
                        if (inner is not null)
                        {
                            rewritten.Add(new ScalarValue(inner));
                            changed = true;
                        }
                        else
                        {
                            rewritten.Add(item);
                        }
                    }
                    return changed ? new SequenceValue(rewritten) : null;
                }
            default:
                return null;
        }
    }
}
