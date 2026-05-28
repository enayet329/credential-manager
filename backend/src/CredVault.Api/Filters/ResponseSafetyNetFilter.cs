using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http.HttpResults;

namespace CredVault.Api.Filters;

/// <summary>
/// Defense-in-depth. Serialises a successful endpoint's outgoing JSON and runs two checks:
/// (1) does any property name match a sensitive pattern AND (2) does its value match a likely-secret
/// regex? If so, throw — the endpoint shouldn't have leaked it. Allowlisted endpoints (value
/// retrieval, decrypted notes, step-up token) bypass the check.
/// </summary>
public sealed partial class ResponseSafetyNetFilter : IEndpointFilter
{
    /// <summary>Marker key set in endpoint metadata to opt out of safety-net checks.</summary>
    public const string AllowlistMetadataKey = "credvault.safety_net_allowlisted";

    private static readonly string[] SensitivePropertyHints =
    [
        "password", "apikey", "api_key", "secret", "secretkey", "secret_key",
        "token", "authorization", "access_key", "private_key", "webhook_secret",
        "master_key", "kek", "dek",
    ];

    [GeneratedRegex(
        @"sk-[A-Za-z0-9_-]{16,}|sk-ant-[A-Za-z0-9_-]{16,}|sk_(live|test)_[A-Za-z0-9]{16,}|pk_(live|test)_[A-Za-z0-9]{16,}|ghp_[A-Za-z0-9]{20,}|AKIA[A-Z0-9]{16}|whsec_[A-Za-z0-9_-]{20,}",
        RegexOptions.CultureInvariant)]
    private static partial Regex LikelySecretRegex();

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <inheritdoc/>
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var result = await next(context).ConfigureAwait(false);

        // Allowlist via endpoint metadata
        var endpoint = context.HttpContext.GetEndpoint();
        if (endpoint?.Metadata.GetMetadata<SafetyNetAllowlistMarker>() is not null)
            return result;

        var body = ExtractValue(result);
        if (body is null) return result;

        var json = JsonSerializer.Serialize(body, JsonOptions);
        if (Scan(json))
            throw new InvalidOperationException(
                "Response safety net tripped: a sensitive-looking value was about to be returned by a non-allowlisted endpoint.");

        return result;
    }

    private static object? ExtractValue(object? result) => result switch
    {
        IValueHttpResult vr => vr.Value,
        _ => null,
    };

    internal static bool Scan(string json)
    {
        // Quick prefilter — only run the value regex on documents that mention a sensitive key.
        var hasHint = false;
        foreach (var hint in SensitivePropertyHints)
        {
            if (json.Contains(hint, StringComparison.OrdinalIgnoreCase))
            {
                hasHint = true;
                break;
            }
        }
        if (!hasHint) return false;

        return LikelySecretRegex().IsMatch(json);
    }
}

/// <summary>Endpoint-metadata marker that opts an endpoint out of <see cref="ResponseSafetyNetFilter"/>.</summary>
public sealed class SafetyNetAllowlistMarker;
