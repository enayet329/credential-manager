using CredVault.Domain.Audit;

namespace CredVault.Application.Abstractions.Persistence;

/// <summary>Append-only repository for <see cref="AuditLog"/>.</summary>
public interface IAuditLogRepository
{
    /// <summary>Inserts a single audit row.</summary>
    Task AppendAsync(AuditLog log, CancellationToken cancellationToken);
}
