using System.Security.Cryptography;
using CredVault.Infrastructure.Cryptography;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CredVault.Infrastructure.Tests.Cryptography;

public class ConfigurationMasterKeyProviderTests
{
    [Fact]
    public void Throws_when_no_keys_configured()
    {
        var options = Options.Create(new MasterKeyOptions { Versions = [] });
        var act = () => new ConfigurationMasterKeyProvider(options, NullLogger<ConfigurationMasterKeyProvider>.Instance);
        act.Should().Throw<InvalidOperationException>().WithMessage("*at least one*");
    }

    [Fact]
    public void Throws_when_key_is_wrong_length()
    {
        var options = Options.Create(new MasterKeyOptions
        {
            Versions = [new MasterKeyVersion { Version = 1, Base64Key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16)) }],
        });
        var act = () => new ConfigurationMasterKeyProvider(options, NullLogger<ConfigurationMasterKeyProvider>.Instance);
        act.Should().Throw<InvalidOperationException>().WithMessage("*exactly 32 bytes*");
    }

    [Fact]
    public void Throws_when_base64_is_invalid()
    {
        var options = Options.Create(new MasterKeyOptions
        {
            Versions = [new MasterKeyVersion { Version = 1, Base64Key = "not-valid-base64!!!" }],
        });
        var act = () => new ConfigurationMasterKeyProvider(options, NullLogger<ConfigurationMasterKeyProvider>.Instance);
        act.Should().Throw<InvalidOperationException>().WithMessage("*not valid Base64*");
    }

    [Fact]
    public void Throws_when_version_zero_or_negative()
    {
        var options = Options.Create(new MasterKeyOptions
        {
            Versions = [new MasterKeyVersion { Version = 0, Base64Key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)) }],
        });
        var act = () => new ConfigurationMasterKeyProvider(options, NullLogger<ConfigurationMasterKeyProvider>.Instance);
        act.Should().Throw<InvalidOperationException>().WithMessage("*positive*");
    }

    [Fact]
    public void Throws_on_duplicate_version()
    {
        var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var options = Options.Create(new MasterKeyOptions
        {
            Versions =
            [
                new MasterKeyVersion { Version = 1, Base64Key = key },
                new MasterKeyVersion { Version = 1, Base64Key = key },
            ],
        });
        var act = () => new ConfigurationMasterKeyProvider(options, NullLogger<ConfigurationMasterKeyProvider>.Instance);
        act.Should().Throw<InvalidOperationException>().WithMessage("*defined more than once*");
    }

    [Fact]
    public void CurrentVersion_is_the_highest_present()
    {
        var keys = new[] { 1, 3, 7 };
        var entries = keys.Select(v => new MasterKeyVersion
        {
            Version = v,
            Base64Key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
        }).ToList();
        var provider = new ConfigurationMasterKeyProvider(
            Options.Create(new MasterKeyOptions { Versions = entries }),
            NullLogger<ConfigurationMasterKeyProvider>.Instance);

        provider.CurrentVersion.Should().Be(7);
        provider.AvailableVersions.Should().Equal([1, 3, 7]);
    }

    [Fact]
    public void GetKey_throws_for_unknown_version()
    {
        var options = Options.Create(new MasterKeyOptions
        {
            Versions = [new MasterKeyVersion { Version = 1, Base64Key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)) }],
        });
        var provider = new ConfigurationMasterKeyProvider(options, NullLogger<ConfigurationMasterKeyProvider>.Instance);
        var act = () => provider.GetKey(99);
        act.Should().Throw<CryptographicException>().WithMessage("*v99*");
    }
}
