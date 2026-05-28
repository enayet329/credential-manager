using CredVault.Domain.Credentials;
using CredVault.Infrastructure.Vault;

namespace CredVault.Infrastructure.Tests.Vault;

public class CredentialFieldValidatorTests
{
    [Fact]
    public void Rejects_unknown_keys()
    {
        var schema = CredentialSchemaRegistry.Get(SupplierType.OpenAI);
        var fields = new Dictionary<string, string> { ["api_key"] = "sk", ["unknown"] = "x" };
        var act = () => CredentialFieldValidator.Validate(schema, fields);
        act.Should().Throw<DomainException>().WithMessage("*Unknown field*");
    }

    [Fact]
    public void Rejects_missing_required()
    {
        var schema = CredentialSchemaRegistry.Get(SupplierType.OpenAI);
        var fields = new Dictionary<string, string>(); // api_key missing
        var act = () => CredentialFieldValidator.Validate(schema, fields);
        act.Should().Throw<DomainException>().WithMessage("*required*");
    }

    [Fact]
    public void Rejects_regex_mismatch()
    {
        var schema = CredentialSchemaRegistry.Get(SupplierType.Postgres);
        var fields = new Dictionary<string, string>
        {
            ["host"] = "db",
            ["port"] = "abc",
            ["username"] = "u",
            ["password"] = "p",
            ["database"] = "d",
        };
        var act = () => CredentialFieldValidator.Validate(schema, fields);
        act.Should().Throw<DomainException>().WithMessage("*pattern*");
    }

    [Fact]
    public void Accepts_complete_payload()
    {
        var schema = CredentialSchemaRegistry.Get(SupplierType.OpenAI);
        var fields = new Dictionary<string, string> { ["api_key"] = "sk-xyz" };
        CredentialFieldValidator.Validate(schema, fields);
    }
}
