using CredVault.Domain.ServiceTokens;

namespace CredVault.Application.Abstractions.Persistence;

/// <summary>Persistence operations on <see cref="ServiceToken"/>.</summary>
public interface IServiceTokenRepository
{
    /// <summary>Fetches a service token by id.</summary>
    Task<ServiceToken?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Looks up a token by exact HMAC match. Used by the auth layer.</summary>
    Task<ServiceToken?> GetByHmacAsync(byte[] hmacHash, CancellationToken cancellationToken);

    /// <summary>Tracks a newly-created token.</summary>
    void Add(ServiceToken token);
}
