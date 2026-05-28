using CredVault.Domain.Credentials;
using Microsoft.EntityFrameworkCore;

namespace CredVault.Infrastructure.Persistence.Repositories;

internal sealed class CredentialRepository : ICredentialRepository
{
    private readonly CredVaultDbContext _context;

    public CredentialRepository(CredVaultDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    public Task<Credential?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _context.Credentials
            .Include(c => c.Rotations)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<Credential?> GetBySlugAsync(Guid environmentId, Guid supplierId, Slug slug, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(slug);
        var slugValue = slug.Value;
        return _context.Credentials
            .FirstOrDefaultAsync(
                c => c.EnvironmentId == environmentId && c.SupplierId == supplierId && c.Slug.Value == slugValue,
                cancellationToken);
    }

    public void Add(Credential credential)
    {
        ArgumentNullException.ThrowIfNull(credential);
        _context.Credentials.Add(credential);
    }
}
