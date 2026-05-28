using CredVault.Domain.Projects;
using CredVault.Domain.Services;
using CredVault.Domain.Suppliers;
using ProjectEnv = CredVault.Domain.Projects.Environment;

namespace CredVault.Domain.Tests.Services;

public class CredentialOrganizationValidatorTests
{
    private readonly FakeClock _clock = new();

    [Fact]
    public void Passes_when_supplier_and_project_share_org_and_env_belongs_to_project()
    {
        var orgId = Guid.NewGuid();
        var project = Project.Create(orgId, "p", Slug.Create("project"), null, _clock);
        var supplier = CredentialSupplier.Create(orgId, SupplierType.OpenAI, "OpenAI", _clock);
        var env = ProjectEnv.Create(project.Id, "Dev", Slug.Create("dev"), EnvironmentType.Development, _clock);

        var act = () => CredentialOrganizationValidator.EnsureSameOrganization(supplier, project, env);
        act.Should().NotThrow();
    }

    [Fact]
    public void Throws_when_supplier_and_project_are_in_different_orgs()
    {
        var project = Project.Create(Guid.NewGuid(), "p", Slug.Create("project"), null, _clock);
        var supplier = CredentialSupplier.Create(Guid.NewGuid(), SupplierType.OpenAI, "OpenAI", _clock);
        var env = ProjectEnv.Create(project.Id, "Dev", Slug.Create("dev"), EnvironmentType.Development, _clock);

        var act = () => CredentialOrganizationValidator.EnsureSameOrganization(supplier, project, env);
        act.Should().Throw<DomainException>().WithMessage("*same organization*");
    }

    [Fact]
    public void Throws_when_environment_belongs_to_a_different_project()
    {
        var orgId = Guid.NewGuid();
        var project = Project.Create(orgId, "p", Slug.Create("project"), null, _clock);
        var supplier = CredentialSupplier.Create(orgId, SupplierType.OpenAI, "OpenAI", _clock);
        var env = ProjectEnv.Create(Guid.NewGuid(), "Dev", Slug.Create("dev"), EnvironmentType.Development, _clock);

        var act = () => CredentialOrganizationValidator.EnsureSameOrganization(supplier, project, env);
        act.Should().Throw<DomainException>().WithMessage("*supplied project*");
    }

    [Fact]
    public void Throws_on_null_arguments()
    {
        var orgId = Guid.NewGuid();
        var project = Project.Create(orgId, "p", Slug.Create("project"), null, _clock);
        var supplier = CredentialSupplier.Create(orgId, SupplierType.OpenAI, "OpenAI", _clock);
        var env = ProjectEnv.Create(project.Id, "Dev", Slug.Create("dev"), EnvironmentType.Development, _clock);

        ((Action)(() => CredentialOrganizationValidator.EnsureSameOrganization(null!, project, env))).Should().Throw<ArgumentNullException>();
        ((Action)(() => CredentialOrganizationValidator.EnsureSameOrganization(supplier, null!, env))).Should().Throw<ArgumentNullException>();
        ((Action)(() => CredentialOrganizationValidator.EnsureSameOrganization(supplier, project, null!))).Should().Throw<ArgumentNullException>();
    }
}
