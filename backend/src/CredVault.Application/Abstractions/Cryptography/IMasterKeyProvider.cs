namespace CredVault.Application.Abstractions.Cryptography;

/// <summary>
/// Provides the set of master key-encryption keys (KEKs) the encryption service can use.
/// Implementations are responsible for keeping plaintext keys out of logs and memory dumps.
/// </summary>
public interface IMasterKeyProvider
{
    /// <summary>The highest-numbered KEK version available. New encryptions use this version.</summary>
    int CurrentVersion { get; }

    /// <summary>All registered versions, ascending. Used for forward-compat decrypt of old ciphertexts.</summary>
    IReadOnlyList<int> AvailableVersions { get; }

    /// <summary>Returns the 32-byte KEK for <paramref name="version"/>. Throws if the version is unknown.</summary>
    byte[] GetKey(int version);
}
