using CredVault.Domain.Projects;
using CredVault.Domain.Suppliers;
using Environment = CredVault.Domain.Projects.Environment;

namespace CredVault.Domain.Services;

/// <summary>
/// Domain service that enforces the cross-aggregate rule: a credential must reference a supplier and
/// an environment that belong to the same organisation.
/// </summary>
public static class CredentialOrganizationValidator
{
    /// <summary>
    /// Throws <see cref="DomainException"/> if <paramref name="supplier"/>, <paramref name="project"/>,
    /// and (optionally) <paramref name="environment"/> do not share an organisation.
    /// </summary>
    public static void EnsureSameOrganization(CredentialSupplier supplier, Project project, Environment environment)
    {
        ArgumentNullException.ThrowIfNull(supplier);
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(environment);

        if (supplier.OrganizationId != project.OrganizationId)
            throw new DomainException("Supplier and project must belong to the same organization.");
        if (environment.ProjectId != project.Id)
            throw new DomainException("Environment must belong to the supplied project.");
    }
}
