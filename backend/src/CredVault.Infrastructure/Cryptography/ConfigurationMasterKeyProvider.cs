using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CredVault.Infrastructure.Cryptography;

/// <summary>
/// Loads master keys from <see cref="MasterKeyOptions"/>. Validates shape eagerly at construction —
/// startup fails loudly if a key is missing or malformed. Plaintext key material never appears in logs.
/// </summary>
public sealed class ConfigurationMasterKeyProvider : IMasterKeyProvider
{
    /// <summary>Required byte length of every KEK after Base64 decode.</summary>
    public const int KeyLengthBytes = 32;

    private static readonly Action<ILogger, int, int, Exception?> LogLoaded =
        LoggerMessage.Define<int, int>(
            LogLevel.Information,
            new EventId(1001, nameof(ConfigurationMasterKeyProvider)),
            "Loaded {Count} master key versions, current = v{Version}");

    private readonly Dictionary<int, byte[]> _keysByVersion;
    private readonly int[] _versionsAscending;

    /// <summary>Loads and validates keys from the supplied options snapshot.</summary>
    public ConfigurationMasterKeyProvider(IOptions<MasterKeyOptions> options, ILogger<ConfigurationMasterKeyProvider> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        var configured = options.Value.Versions ?? [];
        if (configured.Count == 0)
            throw new InvalidOperationException("MasterKey:Versions must contain at least one key.");

        _keysByVersion = new Dictionary<int, byte[]>(capacity: configured.Count);
        foreach (var entry in configured)
        {
            if (entry.Version <= 0)
                throw new InvalidOperationException("MasterKey version numbers must be positive.");
            if (string.IsNullOrWhiteSpace(entry.Base64Key))
                throw new InvalidOperationException($"MasterKey v{entry.Version} has an empty value.");

            byte[] decoded;
            try
            {
                decoded = Convert.FromBase64String(entry.Base64Key);
            }
            catch (FormatException ex)
            {
                throw new InvalidOperationException($"MasterKey v{entry.Version} is not valid Base64.", ex);
            }

            if (decoded.Length != KeyLengthBytes)
                throw new InvalidOperationException(
                    $"MasterKey v{entry.Version} must decode to exactly {KeyLengthBytes} bytes; got {decoded.Length}.");

            if (_keysByVersion.ContainsKey(entry.Version))
                throw new InvalidOperationException($"MasterKey v{entry.Version} is defined more than once.");

            _keysByVersion[entry.Version] = decoded;
        }

        _versionsAscending = [.. _keysByVersion.Keys.OrderBy(v => v)];
        CurrentVersion = _versionsAscending[^1];

        LogLoaded(logger, _versionsAscending.Length, CurrentVersion, null);
    }

    /// <inheritdoc/>
    public int CurrentVersion { get; }

    /// <inheritdoc/>
    public IReadOnlyList<int> AvailableVersions => _versionsAscending;

    /// <inheritdoc/>
    public byte[] GetKey(int version) =>
        _keysByVersion.TryGetValue(version, out var key)
            ? key
            : throw new CryptographicException($"Master key version v{version} is not loaded.");
}
