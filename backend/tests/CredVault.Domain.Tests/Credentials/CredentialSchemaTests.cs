using CredVault.Domain.Credentials;

namespace CredVault.Domain.Tests.Credentials;

public class CredentialSchemaTests
{
    [Fact]
    public void Schema_rejects_empty_fields() =>
        ((Action)(() => new CredentialSchema(SupplierType.Custom, 1, [])))
            .Should().Throw<DomainException>().WithMessage("*at least one field*");

    [Fact]
    public void Schema_rejects_non_positive_version() =>
        ((Action)(() => new CredentialSchema(SupplierType.Custom, 0,
                [new CredentialField("k", "K", FieldType.Text, true, false)])))
            .Should().Throw<DomainException>().WithMessage("*positive*");

    [Fact]
    public void Schema_rejects_duplicate_keys() =>
        ((Action)(() => new CredentialSchema(SupplierType.Custom, 1, [
            new CredentialField("dup", "A", FieldType.Text, true, false),
            new CredentialField("dup", "B", FieldType.Text, true, false),
        ]))).Should().Throw<DomainException>().WithMessage("*Duplicate*");

    [Fact]
    public void Schema_rejects_null_fields() =>
        ((Action)(() => new CredentialSchema(SupplierType.Custom, 1, null!))).Should().Throw<ArgumentNullException>();

    [Theory]
    [InlineData(SupplierType.OpenAI, "api_key")]
    [InlineData(SupplierType.Anthropic, "api_key")]
    [InlineData(SupplierType.AzureOpenAI, "deployment_name")]
    [InlineData(SupplierType.AwsCredentials, "access_key_id")]
    [InlineData(SupplierType.Stripe, "secret_key")]
    [InlineData(SupplierType.GitHub, "personal_access_token")]
    [InlineData(SupplierType.Postgres, "host")]
    [InlineData(SupplierType.GenericApiKey, "api_key")]
    [InlineData(SupplierType.Custom, "payload")]
    public void Registry_returns_seed_schemas(SupplierType type, string expectedKey)
    {
        var schema = CredentialSchemaRegistry.Get(type);
        schema.SupplierType.Should().Be(type);
        schema.Fields.Should().Contain(f => f.Key == expectedKey);
        schema.Version.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Registry_throws_for_unmapped_supplier()
    {
        var act = () => CredentialSchemaRegistry.Get(SupplierType.MongoDb);
        act.Should().Throw<DomainException>().WithMessage("*No schema*");
    }

    [Fact]
    public void Registry_All_returns_every_seeded_schema() =>
        CredentialSchemaRegistry.All.Should().NotBeEmpty();
}
