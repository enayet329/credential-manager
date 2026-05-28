using CredVault.Api.IntegrationTests.TestSupport;

namespace CredVault.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public class SchemaEndpointsTests
{
    private readonly ApiFactory _factory;

    public SchemaEndpointsTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Schema_listing_returns_all_seeded_supplier_types()
    {
        var seed = await TestSeed.CreateAsync(_factory, "schemas-list");
        var client = _factory.AuthedClient(JwtTestIssuer.IssueUser(Guid.NewGuid(), CredVault.Api.Auth.Permissions.ReadMetadata));

        var resp = await client.GetAsync($"/api/v1/orgs/{seed.OrgSlug}/supplier-schemas");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var schemas = await resp.Content.ReadFromJsonAsync<List<CredentialSchemaDto>>();
        schemas.Should().NotBeNull();
        schemas!.Select(s => s.SupplierType).Should().Contain(
        [
            SupplierType.OpenAI, SupplierType.Anthropic, SupplierType.AzureOpenAI,
            SupplierType.AwsCredentials, SupplierType.Stripe, SupplierType.GitHub,
            SupplierType.Postgres, SupplierType.GenericApiKey, SupplierType.Custom,
        ]);

        foreach (var s in schemas!)
        {
            s.Fields.Should().NotBeEmpty();
            foreach (var f in s.Fields)
            {
                f.Key.Should().NotBeNullOrWhiteSpace();
                f.DisplayName.Should().NotBeNullOrWhiteSpace();
            }
        }
    }
}
