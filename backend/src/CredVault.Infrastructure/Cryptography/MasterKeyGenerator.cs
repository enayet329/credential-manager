using System.Security.Cryptography;

namespace CredVault.Infrastructure.Cryptography;

/// <summary>Helper used by the <c>generate-master-key</c> CLI command.</summary>
public static class MasterKeyGenerator
{
    /// <summary>Returns a fresh 32-byte key, Base64-encoded.</summary>
    public static string GenerateBase64()
    {
        var bytes = RandomNumberGenerator.GetBytes(ConfigurationMasterKeyProvider.KeyLengthBytes);
        try
        {
            return Convert.ToBase64String(bytes);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(bytes);
        }
    }
}
