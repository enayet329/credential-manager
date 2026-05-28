using CredVault.Domain.ServiceTokens;
using Microsoft.EntityFrameworkCore;

namespace CredVault.Infrastructure.Persistence.Repositories;

internal sealed class ServiceTokenRepository : IServiceTokenRepository
{
    private readonly CredVaultDbContext _context;

    public ServiceTokenRepository(CredVaultDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    public Task<ServiceToken?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _context.ServiceTokens.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public Task<ServiceToken?> GetByHmacAsync(byte[] hmacHash, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(hmacHash);
        return _context.ServiceTokens.FirstOrDefaultAsync(t => t.HmacHash == hmacHash, cancellationToken);
    }

    public void Add(ServiceToken token)
    {
        ArgumentNullException.ThrowIfNull(token);
        _context.ServiceTokens.Add(token);
    }
}
