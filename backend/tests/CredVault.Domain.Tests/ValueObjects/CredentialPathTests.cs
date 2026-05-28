namespace CredVault.Domain.Tests.ValueObjects;

public class CredentialPathTests
{
    [Fact]
    public void Parse_accepts_four_segments()
    {
        var path = CredentialPath.Parse("acme/staging/openai/primary-key");
        path.Project.Value.Should().Be("acme");
        path.Environment.Value.Should().Be("staging");
        path.Supplier.Value.Should().Be("openai");
        path.Credential.Value.Should().Be("primary-key");
    }

    [Fact]
    public void ToString_round_trips()
    {
        const string raw = "acme/staging/openai/primary-key";
        CredentialPath.Parse(raw).ToString().Should().Be(raw);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_rejects_empty(string raw)
    {
        var act = () => CredentialPath.Parse(raw);
        act.Should().Throw<DomainException>().WithMessage("*empty*");
    }

    [Theory]
    [InlineData("one/two/three")]
    [InlineData("one/two/three/four/five")]
    [InlineData("one")]
    public void Parse_rejects_wrong_segment_count(string raw)
    {
        var act = () => CredentialPath.Parse(raw);
        act.Should().Throw<DomainException>().WithMessage("*four segments*");
    }

    [Fact]
    public void Parse_propagates_slug_validation_errors()
    {
        var act = () => CredentialPath.Parse("acme/STAGING/openai/primary-key");
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_constructs_from_validated_slugs()
    {
        var path = CredentialPath.Create(
            Slug.Create("acme"), Slug.Create("staging"), Slug.Create("openai"), Slug.Create("primary-key"));
        path.ToString().Should().Be("acme/staging/openai/primary-key");
    }
}
