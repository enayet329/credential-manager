using CredVault.Api.Auth;
using CredVault.Api.IntegrationTests.TestSupport;
using CredVault.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CredVault.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public class CredentialFlowTests
{
    private readonly ApiFactory _factory;

    public CredentialFlowTests(ApiFactory factory) => _factory = factory;

    private const string CreateUrlTemplate =
        "/api/v1/orgs/{0}/projects/{1}/environments/{2}/suppliers/{3}/credentials";

    private static string UserToken(Guid userId, params string[] perms) => JwtTestIssuer.IssueUser(userId, perms);

    private static StringContent JsonBody(object body) =>
        new(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

    [Fact]
    public async Task Missing_required_field_returns_400_with_per_field_error()
    {
        var seed = await TestSeed.CreateAsync(_factory, "missing-req", supplierType: SupplierType.OpenAI);
        var user = Guid.NewGuid();
        var client = _factory.AuthedClient(UserToken(user, Permissions.WriteCredentials), JwtTestIssuer.IssueStepUp(user));

        var body = JsonBody(new
        {
            name = "primary-key",
            slug = "primary-key",
            fields = new Dictionary<string, string>(), // api_key missing
        });
        var resp = await client.PostAsync(
            string.Format(CreateUrlTemplate, seed.OrgSlug, seed.ProjectSlug, seed.EnvSlug, seed.SupplierId),
            body);

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var text = await resp.Content.ReadAsStringAsync();
        text.Should().Contain("api_key");
    }

    [Fact]
    public async Task Unknown_field_returns_400()
    {
        var seed = await TestSeed.CreateAsync(_factory, "unknown-field", supplierType: SupplierType.OpenAI);
        var user = Guid.NewGuid();
        var client = _factory.AuthedClient(UserToken(user, Permissions.WriteCredentials), JwtTestIssuer.IssueStepUp(user));

        var body = JsonBody(new
        {
            name = "key",
            slug = "primary-key",
            fields = new Dictionary<string, string> { ["api_key"] = "sk-test", ["unknown_field"] = "x" },
        });
        var resp = await client.PostAsync(
            string.Format(CreateUrlTemplate, seed.OrgSlug, seed.ProjectSlug, seed.EnvSlug, seed.SupplierId),
            body);

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var text = await resp.Content.ReadAsStringAsync();
        text.Should().Contain("Unknown field");
    }

    [Fact]
    public async Task Valid_stripe_credential_returns_201_with_no_secret_values()
    {
        var seed = await TestSeed.CreateAsync(_factory, "stripe-create", supplierType: SupplierType.Stripe);
        var user = Guid.NewGuid();
        var client = _factory.AuthedClient(UserToken(user, Permissions.WriteCredentials), JwtTestIssuer.IssueStepUp(user));

        var fields = new Dictionary<string, string>
        {
            ["publishable_key"] = "pk_test_RealisticPublishableKey1234",
            ["secret_key"] = "sk_test_RealisticSecretValue1234567890",
        };
        var body = JsonBody(new { name = "stripe-prod", slug = "stripe-prod", fields });
        var resp = await client.PostAsync(
            string.Format(CreateUrlTemplate, seed.OrgSlug, seed.ProjectSlug, seed.EnvSlug, seed.SupplierId),
            body);

        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var rawJson = await resp.Content.ReadAsStringAsync();
        rawJson.Should().NotContain("sk_test_RealisticSecretValue1234567890");
        rawJson.Should().NotContain("pk_test_RealisticPublishableKey1234");

        var meta = JsonSerializer.Deserialize<CredentialMetadataDto>(rawJson, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        meta.Should().NotBeNull();
        meta!.MaskedPreview.Should().NotContain("sk_test_RealisticSecretValue");
        meta.MaskedPreview.Should().NotBeNullOrEmpty();
        meta.IsRevoked.Should().BeFalse();
    }

    [Fact]
    public async Task Rotation_without_step_up_returns_403_with_step_up_required_code()
    {
        var seed = await TestSeed.CreateAsync(_factory, "rotate-no-stepup", supplierType: SupplierType.OpenAI);
        var user = Guid.NewGuid();

        // Create with step-up
        var createClient = _factory.AuthedClient(UserToken(user, Permissions.WriteCredentials), JwtTestIssuer.IssueStepUp(user));
        var createResp = await createClient.PostAsync(
            string.Format(CreateUrlTemplate, seed.OrgSlug, seed.ProjectSlug, seed.EnvSlug, seed.SupplierId),
            JsonBody(new { name = "key", slug = "rotate-target", fields = new { api_key = "sk-OriginalValue1234567890" } }));
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var meta = await createResp.Content.ReadFromJsonAsync<CredentialMetadataDto>();
        meta.Should().NotBeNull();

        // Rotate WITHOUT step-up
        var rotateClient = _factory.AuthedClient(UserToken(user, Permissions.WriteCredentials));
        var rotateResp = await rotateClient.PostAsync(
            $"/api/v1/orgs/{seed.OrgSlug}/credentials/{meta!.Id}/rotate",
            JsonBody(new { fields = new { api_key = "sk-NewValue9876543210" } }));

        rotateResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var json = await rotateResp.Content.ReadAsStringAsync();
        json.Should().Contain("step_up_required");
    }

    [Fact]
    public async Task After_step_up_rotation_succeeds_and_snapshots_previous_payload()
    {
        var seed = await TestSeed.CreateAsync(_factory, "rotate-with-stepup", supplierType: SupplierType.OpenAI);
        var user = Guid.NewGuid();
        var client = _factory.AuthedClient(UserToken(user, Permissions.WriteCredentials), JwtTestIssuer.IssueStepUp(user));

        var createResp = await client.PostAsync(
            string.Format(CreateUrlTemplate, seed.OrgSlug, seed.ProjectSlug, seed.EnvSlug, seed.SupplierId),
            JsonBody(new { name = "key", slug = "rotate-history", fields = new { api_key = "sk-OldValueXXXXXXXXX" } }));
        var meta = await createResp.Content.ReadFromJsonAsync<CredentialMetadataDto>();

        // Capture old encrypted payload directly from DB.
        byte[] oldCiphertext;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<CredVaultDbContext>();
            oldCiphertext = (await ctx.Credentials.AsNoTracking().FirstAsync(c => c.Id == meta!.Id)).EncryptedPayload;
        }

        var rotateResp = await client.PostAsync(
            $"/api/v1/orgs/{seed.OrgSlug}/credentials/{meta!.Id}/rotate",
            JsonBody(new { fields = new { api_key = "sk-NewValueYYYYYYYYY" }, reason = "scheduled" }));
        rotateResp.StatusCode.Should().Be(HttpStatusCode.OK);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<CredVaultDbContext>();
            var rotations = await ctx.CredentialRotations.AsNoTracking()
                .Where(r => r.CredentialId == meta.Id).ToListAsync();
            rotations.Should().ContainSingle();
            rotations[0].PreviousEncryptedPayload.Should().Equal(oldCiphertext);
            rotations[0].Reason.Should().Be("scheduled");
        }
    }

    [Fact]
    public async Task Listing_returns_masked_preview_not_full_secret()
    {
        var seed = await TestSeed.CreateAsync(_factory, "list-mask", supplierType: SupplierType.OpenAI);
        var user = Guid.NewGuid();
        var write = _factory.AuthedClient(UserToken(user, Permissions.WriteCredentials), JwtTestIssuer.IssueStepUp(user));
        await write.PostAsync(
            string.Format(CreateUrlTemplate, seed.OrgSlug, seed.ProjectSlug, seed.EnvSlug, seed.SupplierId),
            JsonBody(new { name = "k", slug = "list-mask", fields = new { api_key = "sk-PrivateValue1234567890" } }));

        var read = _factory.AuthedClient(UserToken(Guid.NewGuid(), Permissions.ReadMetadata));
        var listResp = await read.GetAsync($"/api/v1/orgs/{seed.OrgSlug}/credentials");
        listResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var listJson = await listResp.Content.ReadAsStringAsync();

        listJson.Should().NotContain("sk-PrivateValue1234567890");
        listJson.Should().Contain("\"maskedPreview\"");
    }

    [Fact]
    public async Task Path_based_retrieval_works_and_writes_access_log()
    {
        var seed = await TestSeed.CreateAsync(_factory, "path-read", supplierType: SupplierType.OpenAI);
        var writer = Guid.NewGuid();
        var write = _factory.AuthedClient(UserToken(writer, Permissions.WriteCredentials), JwtTestIssuer.IssueStepUp(writer));
        var createResp = await write.PostAsync(
            string.Format(CreateUrlTemplate, seed.OrgSlug, seed.ProjectSlug, seed.EnvSlug, seed.SupplierId),
            JsonBody(new { name = "primary", slug = "primary-path", fields = new { api_key = "sk-PathValue1234567890" } }));
        var meta = await createResp.Content.ReadFromJsonAsync<CredentialMetadataDto>();

        var reader = Guid.NewGuid();
        var read = _factory.AuthedClient(UserToken(reader, Permissions.ReadValue));
        var resp = await read.GetAsync(
            $"/api/v1/orgs/{seed.OrgSlug}/credentials/by-path/{seed.ProjectSlug}/{seed.EnvSlug}/{seed.SupplierSlug}/primary-path/value");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var value = await resp.Content.ReadFromJsonAsync<CredentialValueResponse>();
        value!.Fields["api_key"].Should().Be("sk-PathValue1234567890");

        await using var scope = _factory.Services.CreateAsyncScope();
        var ctx = scope.ServiceProvider.GetRequiredService<CredVaultDbContext>();
        var logs = await ctx.CredentialAccessLogs.AsNoTracking()
            .Where(l => l.CredentialId == meta!.Id && l.ActorId == reader).ToListAsync();
        logs.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Rotation_history_endpoint_never_exposes_old_values()
    {
        var seed = await TestSeed.CreateAsync(_factory, "rotation-history", supplierType: SupplierType.OpenAI);
        var user = Guid.NewGuid();
        var client = _factory.AuthedClient(UserToken(user, Permissions.WriteCredentials, Permissions.ReadMetadata), JwtTestIssuer.IssueStepUp(user));

        const string secret = "sk-OldSecretShouldNeverLeak123";
        var createResp = await client.PostAsync(
            string.Format(CreateUrlTemplate, seed.OrgSlug, seed.ProjectSlug, seed.EnvSlug, seed.SupplierId),
            JsonBody(new { name = "x", slug = "rot-hist", fields = new { api_key = secret } }));
        var meta = await createResp.Content.ReadFromJsonAsync<CredentialMetadataDto>();
        await client.PostAsync(
            $"/api/v1/orgs/{seed.OrgSlug}/credentials/{meta!.Id}/rotate",
            JsonBody(new { fields = new { api_key = "sk-NewSecretXXX" }, reason = "manual" }));

        var historyResp = await client.GetAsync($"/api/v1/orgs/{seed.OrgSlug}/credentials/{meta.Id}/rotations");
        historyResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var text = await historyResp.Content.ReadAsStringAsync();
        text.Should().NotContain(secret);
        text.Should().NotContain("PreviousEncryptedPayload");
        text.Should().Contain("manual");
    }

    [Fact]
    public async Task Credential_note_create_read_delete_round_trip()
    {
        var seed = await TestSeed.CreateAsync(_factory, "notes", supplierType: SupplierType.OpenAI);
        var user = Guid.NewGuid();
        var write = _factory.AuthedClient(UserToken(user, Permissions.WriteCredentials, Permissions.ReadValue), JwtTestIssuer.IssueStepUp(user));
        var createResp = await write.PostAsync(
            string.Format(CreateUrlTemplate, seed.OrgSlug, seed.ProjectSlug, seed.EnvSlug, seed.SupplierId),
            JsonBody(new { name = "k", slug = "notes-cred", fields = new { api_key = "sk-NotesXXXXXXXXX" } }));
        var meta = await createResp.Content.ReadFromJsonAsync<CredentialMetadataDto>();

        var noteResp = await write.PostAsync(
            $"/api/v1/orgs/{seed.OrgSlug}/credentials/{meta!.Id}/notes",
            JsonBody(new { content = "To rotate this, log into the OpenAI dashboard." }));
        noteResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var note = await noteResp.Content.ReadFromJsonAsync<CredentialNoteDto>();
        note!.Content.Should().Be("To rotate this, log into the OpenAI dashboard.");

        var listResp = await write.GetAsync($"/api/v1/orgs/{seed.OrgSlug}/credentials/{meta.Id}/notes");
        listResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var notes = await listResp.Content.ReadFromJsonAsync<List<CredentialNoteDto>>();
        notes.Should().ContainSingle();
        notes![0].Content.Should().Be("To rotate this, log into the OpenAI dashboard.");

        var deleteResp = await write.DeleteAsync($"/api/v1/orgs/{seed.OrgSlug}/credentials/{meta.Id}/notes/{note.Id}");
        deleteResp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Admin_registers_new_schema_then_credential_create_and_retrieve_work_end_to_end()
    {
        var seed = await TestSeed.CreateAsync(_factory, "admin-schema", supplierType: SupplierType.MongoDb);
        // MongoDB has no built-in schema; the admin endpoint registers one at runtime.
        var admin = _factory.AuthedClient(UserToken(Guid.NewGuid(), Permissions.AdminSchemas));
        var registerResp = await admin.PostAsync("/api/admin/credential-schemas", JsonBody(new
        {
            supplierType = SupplierType.MongoDb,
            version = 1,
            fields = new[]
            {
                new { key = "connection_string", displayName = "Connection string", fieldType = FieldType.Password, isRequired = true, isSecret = true },
            },
        }));
        registerResp.StatusCode.Should().Be(HttpStatusCode.Created);

        var user = Guid.NewGuid();
        var write = _factory.AuthedClient(UserToken(user, Permissions.WriteCredentials, Permissions.ReadValue), JwtTestIssuer.IssueStepUp(user));
        var createResp = await write.PostAsync(
            string.Format(CreateUrlTemplate, seed.OrgSlug, seed.ProjectSlug, seed.EnvSlug, seed.SupplierId),
            JsonBody(new
            {
                name = "mongo",
                slug = "mongo-cluster",
                fields = new { connection_string = "mongodb+srv://u:p@cluster.example.com/db" },
            }));
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var meta = await createResp.Content.ReadFromJsonAsync<CredentialMetadataDto>();

        var valueResp = await write.GetAsync($"/api/v1/orgs/{seed.OrgSlug}/credentials/{meta!.Id}/value");
        valueResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var value = await valueResp.Content.ReadFromJsonAsync<CredentialValueResponse>();
        value!.Fields["connection_string"].Should().Be("mongodb+srv://u:p@cluster.example.com/db");
    }

    [Fact]
    public async Task Viewer_role_gets_403_on_value_but_200_on_metadata()
    {
        var seed = await TestSeed.CreateAsync(_factory, "viewer-rbac", supplierType: SupplierType.OpenAI);
        var writer = Guid.NewGuid();
        var write = _factory.AuthedClient(UserToken(writer, Permissions.WriteCredentials), JwtTestIssuer.IssueStepUp(writer));
        var createResp = await write.PostAsync(
            string.Format(CreateUrlTemplate, seed.OrgSlug, seed.ProjectSlug, seed.EnvSlug, seed.SupplierId),
            JsonBody(new { name = "k", slug = "viewer-test", fields = new { api_key = "sk-ViewerSecret123456" } }));
        var meta = await createResp.Content.ReadFromJsonAsync<CredentialMetadataDto>();

        // Viewer: only credentials:read:metadata
        var viewer = _factory.AuthedClient(UserToken(Guid.NewGuid(), Permissions.ReadMetadata));
        var metaResp = await viewer.GetAsync($"/api/v1/orgs/{seed.OrgSlug}/credentials/{meta!.Id}");
        metaResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var valueResp = await viewer.GetAsync($"/api/v1/orgs/{seed.OrgSlug}/credentials/{meta.Id}/value");
        valueResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
