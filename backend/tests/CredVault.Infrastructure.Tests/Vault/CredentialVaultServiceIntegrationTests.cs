using CredVault.Domain.Organizations;
using CredVault.Domain.Projects;
using CredVault.Domain.Suppliers;
using CredVault.Infrastructure.Cryptography;
using CredVault.Infrastructure.Persistence;
using CredVault.Infrastructure.Persistence.Repositories;
using CredVault.Infrastructure.RateLimiting;
using CredVault.Infrastructure.Schemas;
using CredVault.Infrastructure.Tests.TestSupport;
using CredVault.Infrastructure.Vault;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Environment = CredVault.Domain.Projects.Environment;

namespace CredVault.Infrastructure.Tests.Vault;

[Collection(SqlServerCollection.Name)]
public class CredentialVaultServiceIntegrationTests
{
    private readonly SqlServerFixture _sql;
    private readonly FakeClock _clock = new();

    public CredentialVaultServiceIntegrationTests(SqlServerFixture sql) => _sql = sql;

    [Fact]
    public async Task Store_then_Retrieve_roundtrips_and_writes_an_access_log()
    {
        await using var ctx = _sql.CreateContext();
        var (orgId, supplierId, envId) = await SeedAsync(ctx, slugSuffix: "store");

        var (provider, _) = InMemoryMasterKeys.Build(1);
        var encryption = new EnvelopeEncryptionService(provider);
        var schemas = new CredentialSchemaProvider();
        var accessLogs = new CredentialAccessLogRepository(ctx);
        await using var rateLimiter = new InProcessRateLimiter(Options.Create(new VaultRateLimitOptions { PerCredentialPerMinute = 60 }));
        using var plaintextCache = new PlaintextCache(Options.Create(new PlaintextCacheOptions { Enabled = false }));

        var service = new CredentialVaultService(
            ctx, encryption, schemas, accessLogs, _clock, rateLimiter, plaintextCache,
            Options.Create(new VaultRateLimitOptions { PerCredentialPerMinute = 60 }),
            NullLogger<CredentialVaultService>.Instance);

        var fields = new Dictionary<string, string>
        {
            ["api_key"] = "sk-test-1234567890ABCDEF",
            ["organization_id"] = "org-xyz",
        };

        var credId = await service.StoreAsync(supplierId, envId, "primary-key", Slug.Create("primary-key"), fields, expiresAtUtc: null, CancellationToken.None);

        var access = new CredentialAccessContext(ActorType.User, Guid.NewGuid(), "10.0.0.1", "tests", AccessMethod.Cli);
        var read = await service.RetrieveAsync(credId, access, CancellationToken.None);

        read["api_key"].Should().Be("sk-test-1234567890ABCDEF");
        read["organization_id"].Should().Be("org-xyz");

        // Access log row attributed to the right actor + outcome
        var logs = await ctx.CredentialAccessLogs.Where(l => l.CredentialId == credId).ToListAsync();
        logs.Should().ContainSingle();
        logs[0].ActorId.Should().Be(access.ActorId);
        logs[0].Outcome.Should().Be(AccessOutcome.Success);
        logs[0].AccessMethod.Should().Be(AccessMethod.Cli);

        // The credential's access counters were bumped
        var credential = await ctx.Credentials.FirstAsync(c => c.Id == credId);
        credential.AccessCount.Should().Be(1);
        credential.LastAccessedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task Rotate_snapshots_previous_payload_into_CredentialRotation()
    {
        Guid supplierId;
        Guid envId;
        await using (var seedCtx = _sql.CreateContext())
        {
            (_, supplierId, envId) = await SeedAsync(seedCtx, slugSuffix: "rotate");
        }

        var (provider, _) = InMemoryMasterKeys.Build(1);
        var encryption = new EnvelopeEncryptionService(provider);
        var schemas = new CredentialSchemaProvider();
        await using var rateLimiter = new InProcessRateLimiter(Options.Create(new VaultRateLimitOptions { PerCredentialPerMinute = 60 }));
        using var plaintextCache = new PlaintextCache(Options.Create(new PlaintextCacheOptions { Enabled = false }));

        Guid credId;
        byte[] previousCiphertext;
        int previousKek;

        await using (var storeCtx = _sql.CreateContext())
        {
            var storeService = new CredentialVaultService(
                storeCtx, encryption, schemas, new CredentialAccessLogRepository(storeCtx), _clock, rateLimiter, plaintextCache,
                Options.Create(new VaultRateLimitOptions { PerCredentialPerMinute = 60 }),
                NullLogger<CredentialVaultService>.Instance);

            credId = await storeService.StoreAsync(supplierId, envId, "primary-key", Slug.Create("primary-key"),
                new Dictionary<string, string> { ["api_key"] = "sk-OLD-1234567890" }, null, CancellationToken.None);
        }

        await using (var inspectCtx = _sql.CreateContext())
        {
            var beforeRotate = await inspectCtx.Credentials.AsNoTracking().FirstAsync(c => c.Id == credId);
            previousCiphertext = beforeRotate.EncryptedPayload;
            previousKek = beforeRotate.KekVersion;
        }

        await using (var rotateCtx = _sql.CreateContext(line => Console.WriteLine("[EF] " + line)))
        {
            var rotateService = new CredentialVaultService(
                rotateCtx, encryption, schemas, new CredentialAccessLogRepository(rotateCtx), _clock, rateLimiter, plaintextCache,
                Options.Create(new VaultRateLimitOptions { PerCredentialPerMinute = 60 }),
                NullLogger<CredentialVaultService>.Instance);

            await rotateService.RotateAsync(credId,
                new Dictionary<string, string> { ["api_key"] = "sk-NEW-fedcba0987" }, null, "scheduled", CancellationToken.None);
        }

        await using (var verifyCtx = _sql.CreateContext())
        {
            var rotations = await verifyCtx.CredentialRotations.Where(r => r.CredentialId == credId).ToListAsync();
            rotations.Should().ContainSingle();
            rotations[0].Reason.Should().Be("scheduled");
            rotations[0].PreviousEncryptedPayload.Should().Equal(previousCiphertext);
            rotations[0].PreviousKekVersion.Should().Be(previousKek);

            var afterRotate = await verifyCtx.Credentials.AsNoTracking().FirstAsync(c => c.Id == credId);
            afterRotate.EncryptedPayload.Should().NotEqual(previousCiphertext);
        }

        // Decrypt still works with the rotated material
        await using var readCtx = _sql.CreateContext();
        var readService = new CredentialVaultService(
            readCtx, encryption, schemas, new CredentialAccessLogRepository(readCtx), _clock, rateLimiter, plaintextCache,
            Options.Create(new VaultRateLimitOptions { PerCredentialPerMinute = 60 }),
            NullLogger<CredentialVaultService>.Instance);
        var access = new CredentialAccessContext(ActorType.User, Guid.NewGuid(), "10.0.0.1", "tests", AccessMethod.UI);
        var read = await readService.RetrieveAsync(credId, access, CancellationToken.None);
        read["api_key"].Should().Be("sk-NEW-fedcba0987");
    }

    [Fact]
    public async Task After_kek_rotation_new_credentials_use_v2_but_old_still_decrypt()
    {
        await using var ctx = _sql.CreateContext();
        var (_, supplierId, envId) = await SeedAsync(ctx, slugSuffix: "kek");

        // v1-only world
        var (v1Provider, v1Raws) = InMemoryMasterKeys.Build(1);
        var v1Encryption = new EnvelopeEncryptionService(v1Provider);
        var schemas = new CredentialSchemaProvider();
        var accessLogs = new CredentialAccessLogRepository(ctx);
        await using var rateLimiter = new InProcessRateLimiter(Options.Create(new VaultRateLimitOptions { PerCredentialPerMinute = 60 }));
        using var plaintextCache = new PlaintextCache(Options.Create(new PlaintextCacheOptions { Enabled = false }));

        var v1Service = new CredentialVaultService(
            ctx, v1Encryption, schemas, accessLogs, _clock, rateLimiter, plaintextCache,
            Options.Create(new VaultRateLimitOptions { PerCredentialPerMinute = 60 }),
            NullLogger<CredentialVaultService>.Instance);

        var oldId = await v1Service.StoreAsync(supplierId, envId, "old-key", Slug.Create("old-key"),
            new Dictionary<string, string> { ["api_key"] = "sk-v1-VALUE" }, null, CancellationToken.None);
        (await ctx.Credentials.AsNoTracking().FirstAsync(c => c.Id == oldId)).KekVersion.Should().Be(1);

        // Add v2 alongside v1
        var v2Options = Options.Create(new MasterKeyOptions
        {
            Versions =
            [
                new MasterKeyVersion { Version = 1, Base64Key = Convert.ToBase64String(v1Raws[0]) },
                new MasterKeyVersion { Version = 2, Base64Key = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)) },
            ],
        });
        var v2Provider = new ConfigurationMasterKeyProvider(v2Options, NullLogger<ConfigurationMasterKeyProvider>.Instance);
        var v2Encryption = new EnvelopeEncryptionService(v2Provider);

        await using var ctx2 = _sql.CreateContext();
        var v2Service = new CredentialVaultService(
            ctx2, v2Encryption, schemas, new CredentialAccessLogRepository(ctx2), _clock, rateLimiter, plaintextCache,
            Options.Create(new VaultRateLimitOptions { PerCredentialPerMinute = 60 }),
            NullLogger<CredentialVaultService>.Instance);

        // NEW credential uses v2
        var newId = await v2Service.StoreAsync(supplierId, envId, "new-key", Slug.Create("new-key"),
            new Dictionary<string, string> { ["api_key"] = "sk-v2-VALUE" }, null, CancellationToken.None);
        (await ctx2.Credentials.AsNoTracking().FirstAsync(c => c.Id == newId)).KekVersion.Should().Be(2);

        // OLD credential still decrypts under v2-aware service
        var access = new CredentialAccessContext(ActorType.User, Guid.NewGuid(), "ip", "ua", AccessMethod.UI);
        var read = await v2Service.RetrieveAsync(oldId, access, CancellationToken.None);
        read["api_key"].Should().Be("sk-v1-VALUE");
    }

    private async Task<(Guid orgId, Guid supplierId, Guid envId)> SeedAsync(CredVaultDbContext ctx, string? slugSuffix = null)
    {
        var orgSlug = slugSuffix is null ? "acme" : $"acme-{slugSuffix}";
        var projSlug = slugSuffix is null ? "app" : $"app-{slugSuffix}";
        var envSlug = slugSuffix is null ? "prod" : $"prod-{slugSuffix}";

        var org = Organization.Create("Acme", Slug.Create(orgSlug), _clock);
        var project = Project.Create(org.Id, "App", Slug.Create(projSlug), null, _clock);
        var env = Environment.Create(project.Id, "Prod", Slug.Create(envSlug), EnvironmentType.Production, _clock);
        var supplier = CredentialSupplier.Create(org.Id, SupplierType.OpenAI, "OpenAI", _clock);

        ctx.Organizations.Add(org);
        ctx.Projects.Add(project);
        ctx.Environments.Add(env);
        ctx.CredentialSuppliers.Add(supplier);
        await ctx.SaveChangesAsync();

        return (org.Id, supplier.Id, env.Id);
    }
}
