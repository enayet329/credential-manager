using CredVault.Domain.Organizations;
using CredVault.Domain.Projects;
using CredVault.Domain.Suppliers;
using CredVault.Domain.ValueObjects;
using CredVault.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProjectEnv = CredVault.Domain.Projects.Environment;

namespace CredVault.Api.IntegrationTests.TestSupport;

/// <summary>Pre-populates an org + project + environment + supplier for tests that need them.</summary>
public sealed record SeedFixture(
    Guid OrgId,
    string OrgSlug,
    Guid ProjectId,
    string ProjectSlug,
    Guid EnvironmentId,
    string EnvSlug,
    Guid SupplierId,
    SupplierType SupplierType,
    string SupplierSlug);

public static class TestSeed
{
    public static async Task<SeedFixture> CreateAsync(
        ApiFactory factory,
        string suffix,
        EnvironmentType envType = EnvironmentType.Development,
        SupplierType supplierType = SupplierType.OpenAI)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var ctx = scope.ServiceProvider.GetRequiredService<CredVaultDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();

        var orgSlug = $"acme-{suffix}";
        var projectSlug = $"app-{suffix}";
        var envSlug = $"env-{suffix}";

        var org = Organization.Create($"Acme {suffix}", Slug.Create(orgSlug), clock);
        var project = Project.Create(org.Id, $"App {suffix}", Slug.Create(projectSlug), null, clock);
        var env = ProjectEnv.Create(project.Id, $"Env {suffix}", Slug.Create(envSlug), envType, clock);
        var supplier = CredentialSupplier.Create(org.Id, supplierType, $"{supplierType} {suffix}", clock);

        ctx.Organizations.Add(org);
        ctx.Projects.Add(project);
        ctx.Environments.Add(env);
        ctx.CredentialSuppliers.Add(supplier);
        await ctx.SaveChangesAsync();

        return new SeedFixture(
            org.Id, orgSlug,
            project.Id, projectSlug,
            env.Id, envSlug,
            supplier.Id, supplierType,
            CredVault.Api.Lookups.SlugLookup.ToSlug(supplierType));
    }
}
