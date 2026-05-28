using CredVault.Domain.Audit;

namespace CredVault.Infrastructure.Persistence.Repositories;

internal sealed class AuditLogRepository : IAuditLogRepository
{
    private readonly CredVaultDbContext _context;

    public AuditLogRepository(CredVaultDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    public async Task AppendAsync(AuditLog log, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(log);
        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
