using CredVault.Domain.Credentials;

namespace CredVault.Application.Abstractions.Persistence;

/// <summary>Append-only repository for <see cref="CredentialAccessLog"/>. Append-only by contract — no update or delete.</summary>
public interface ICredentialAccessLogRepository
{
    /// <summary>Inserts a single row.</summary>
    Task AppendAsync(CredentialAccessLog log, CancellationToken cancellationToken);

    /// <summary>High-throughput bulk insert via <c>SqlBulkCopy</c>. Used by batch flushers.</summary>
    Task BulkInsertAsync(IReadOnlyCollection<CredentialAccessLog> logs, CancellationToken cancellationToken);

    /// <summary>Recent access rows for a credential, newest first.</summary>
    Task<IReadOnlyList<CredentialAccessLog>> GetRecentAsync(Guid credentialId, int take, CancellationToken cancellationToken);
}
