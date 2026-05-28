using System.Security.Cryptography;
using System.Text;
using CredVault.Infrastructure.Cryptography;
using CredVault.Infrastructure.Tests.TestSupport;

namespace CredVault.Infrastructure.Tests.Cryptography;

public class EnvelopeEncryptionServiceTests
{
    [Fact]
    public async Task Roundtrip_decrypts_to_original_plaintext()
    {
        var (provider, _) = InMemoryMasterKeys.Build(1);
        var service = new EnvelopeEncryptionService(provider);
        var plaintext = Encoding.UTF8.GetBytes("hello, vault");
        const string aad = "org:abc|env:def|supplier:ghi|cred:jkl";

        var encrypted = await service.EncryptAsync(plaintext, aad, CancellationToken.None);
        var decrypted = await service.DecryptAsync(encrypted, aad, CancellationToken.None);

        decrypted.Should().Equal(plaintext);
        encrypted.Ciphertext.Should().NotEqual(plaintext);
        encrypted.KekVersion.Should().Be(1);
    }

    [Fact]
    public async Task Wrong_aad_throws_CryptographicException()
    {
        var (provider, _) = InMemoryMasterKeys.Build(1);
        var service = new EnvelopeEncryptionService(provider);
        var plaintext = Encoding.UTF8.GetBytes("secret-token");

        var encrypted = await service.EncryptAsync(plaintext, "org:A|cred:1", CancellationToken.None);

        var act = async () => await service.DecryptAsync(encrypted, "org:A|cred:2", CancellationToken.None);
        await act.Should().ThrowAsync<CryptographicException>();
    }

    [Fact]
    public async Task Older_kek_version_still_decrypts_after_rotation()
    {
        var (oldProvider, oldRaws) = InMemoryMasterKeys.Build(1);
        var oldService = new EnvelopeEncryptionService(oldProvider);

        var plaintext = Encoding.UTF8.GetBytes("ancient");
        const string aad = "ctx";
        var encryptedV1 = await oldService.EncryptAsync(plaintext, aad, CancellationToken.None);
        encryptedV1.KekVersion.Should().Be(1);

        // Rotate: introduce v2 alongside v1
        var newOptions = Microsoft.Extensions.Options.Options.Create(new MasterKeyOptions
        {
            Versions =
            [
                new MasterKeyVersion { Version = 1, Base64Key = Convert.ToBase64String(oldRaws[0]) },
                new MasterKeyVersion { Version = 2, Base64Key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)) },
            ],
        });
        var rotated = new ConfigurationMasterKeyProvider(newOptions, Microsoft.Extensions.Logging.Abstractions.NullLogger<ConfigurationMasterKeyProvider>.Instance);
        var rotatedService = new EnvelopeEncryptionService(rotated);

        rotated.CurrentVersion.Should().Be(2);

        // Existing v1 ciphertext still decrypts
        var decrypted = await rotatedService.DecryptAsync(encryptedV1, aad, CancellationToken.None);
        decrypted.Should().Equal(plaintext);

        // New encryptions use v2
        var encryptedV2 = await rotatedService.EncryptAsync(plaintext, aad, CancellationToken.None);
        encryptedV2.KekVersion.Should().Be(2);
    }

    [Fact]
    public async Task Decrypt_with_unknown_kek_version_throws()
    {
        var (provider, _) = InMemoryMasterKeys.Build(1);
        var service = new EnvelopeEncryptionService(provider);
        var plaintext = Encoding.UTF8.GetBytes("hi");
        var encrypted = await service.EncryptAsync(plaintext, "ctx", CancellationToken.None);

        var rebuilt = new EncryptedPayload(encrypted.Ciphertext, encrypted.WrappedDataKey, encrypted.Nonce, encrypted.AuthTag, KekVersion: 99);
        var act = async () => await service.DecryptAsync(rebuilt, "ctx", CancellationToken.None);
        await act.Should().ThrowAsync<CryptographicException>();
    }

    [Fact]
    public async Task Cancellation_is_observed()
    {
        var (provider, _) = InMemoryMasterKeys.Build(1);
        var service = new EnvelopeEncryptionService(provider);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var act = async () => await service.EncryptAsync([0x1], "ctx", cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
