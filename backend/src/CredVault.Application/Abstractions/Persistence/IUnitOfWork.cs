namespace CredVault.Application.Abstractions.Persistence;

/// <summary>Wraps a transactional <c>SaveChangesAsync</c> against the underlying store.</summary>
public interface IUnitOfWork
{
    /// <summary>Persists pending changes. Returns the number of state entries written.</summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
