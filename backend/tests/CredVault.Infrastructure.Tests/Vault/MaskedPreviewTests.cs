using CredVault.Domain.Credentials;
using CredVault.Infrastructure.Vault;

namespace CredVault.Infrastructure.Tests.Vault;

public class MaskedPreviewTests
{
    [Fact]
    public void Picks_first_secret_field_when_long_enough()
    {
        var schema = CredentialSchemaRegistry.Get(SupplierType.OpenAI);
        var fields = new Dictionary<string, string> { ["api_key"] = "sk-abcdef1234567890" };
        MaskedPreview.Compute(schema, fields).Should().Be("sk-a…7890");
    }

    [Fact]
    public void Falls_back_to_first_non_empty_when_no_long_secret()
    {
        var schema = CredentialSchemaRegistry.Get(SupplierType.Custom);
        var fields = new Dictionary<string, string> { ["payload"] = "short" };
        MaskedPreview.Compute(schema, fields).Should().Be("short");
    }

    [Fact]
    public void Returns_empty_when_no_fields_present()
    {
        var schema = CredentialSchemaRegistry.Get(SupplierType.OpenAI);
        MaskedPreview.Compute(schema, new Dictionary<string, string>()).Should().BeEmpty();
    }
}
