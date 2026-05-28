using CredVault.Infrastructure.Cryptography;

namespace CredVault.Infrastructure.Tests.Cryptography;

public class MasterKeyGeneratorTests
{
    [Fact]
    public void GenerateBase64_produces_a_decodable_32_byte_key()
    {
        var key = MasterKeyGenerator.GenerateBase64();
        key.Should().NotBeNullOrWhiteSpace();
        var bytes = Convert.FromBase64String(key);
        bytes.Length.Should().Be(32);
    }

    [Fact]
    public void GenerateBase64_returns_distinct_values()
    {
        var a = MasterKeyGenerator.GenerateBase64();
        var b = MasterKeyGenerator.GenerateBase64();
        a.Should().NotBe(b);
    }
}
