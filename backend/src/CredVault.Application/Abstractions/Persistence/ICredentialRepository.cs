using CredVault.Domain.Credentials;

namespace CredVault.Application.Abstractions.Persistence;

/// <summary>Persistence operations on the <see cref="Credential"/> aggregate.</summary>
public interface ICredentialRepository
{
    /// <summary>Fetches a credential by id, including its rotation history. Returns <c>null</c> if not found.</summary>
    Task<Credential?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Fetches a credential by (environment, supplier, slug). Returns <c>null</c> if not found.</summary>
    Task<Credential?> GetBySlugAsync(Guid environmentId, Guid supplierId, Slug slug, CancellationToken cancellationToken);

    /// <summary>Tracks a newly-created credential. Caller is responsible for committing via <see cref="IUnitOfWork"/>.</summary>
    void Add(Credential credential);
}
