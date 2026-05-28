using System.Security.Cryptography;
using CredVault.Infrastructure.Cryptography;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CredVault.Infrastructure.Tests.TestSupport;

internal static class InMemoryMasterKeys
{
    public static (ConfigurationMasterKeyProvider Provider, byte[][] RawKeys) Build(params int[] versions)
    {
        var raws = new byte[versions.Length][];
        var entries = new List<MasterKeyVersion>(versions.Length);
        for (var i = 0; i < versions.Length; i++)
        {
            raws[i] = RandomNumberGenerator.GetBytes(32);
            entries.Add(new MasterKeyVersion { Version = versions[i], Base64Key = Convert.ToBase64String(raws[i]) });
        }

        var options = Options.Create(new MasterKeyOptions { Versions = entries });
        var provider = new ConfigurationMasterKeyProvider(options, NullLogger<ConfigurationMasterKeyProvider>.Instance);
        return (provider, raws);
    }
}
