using System.Data;
using CredVault.Domain.Credentials;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CredVault.Infrastructure.Persistence.Repositories;

internal sealed class CredentialAccessLogRepository : ICredentialAccessLogRepository
{
    private readonly CredVaultDbContext _context;

    public CredentialAccessLogRepository(CredVaultDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    public async Task AppendAsync(CredentialAccessLog log, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(log);
        _context.CredentialAccessLogs.Add(log);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task<IReadOnlyList<CredentialAccessLog>> GetRecentAsync(Guid credentialId, int take, CancellationToken cancellationToken) =>
        _context.CredentialAccessLogs
            .Where(l => l.CredentialId == credentialId)
            .OrderByDescending(l => l.AccessedAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<CredentialAccessLog>)t.Result, cancellationToken, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);

    public async Task BulkInsertAsync(IReadOnlyCollection<CredentialAccessLog> logs, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(logs);
        if (logs.Count == 0) return;

        var connectionString = _context.Database.GetConnectionString()
            ?? throw new InvalidOperationException("Connection string is not configured on the DbContext.");

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        using var table = new DataTable();
        table.Columns.Add("Id", typeof(Guid));
        table.Columns.Add("CredentialId", typeof(Guid));
        table.Columns.Add("AccessedAtUtc", typeof(DateTime));
        table.Columns.Add("ActorType", typeof(int));
        table.Columns.Add("ActorId", typeof(Guid));
        table.Columns.Add("IpAddress", typeof(string));
        table.Columns.Add("UserAgent", typeof(string));
        table.Columns.Add("AccessMethod", typeof(int));
        table.Columns.Add("Outcome", typeof(int));
        table.Columns.Add("OrganizationId", typeof(Guid));

        foreach (var log in logs)
        {
            var orgId = ResolveOrganizationId(log);
            table.Rows.Add(
                log.Id == Guid.Empty ? Guid.NewGuid() : log.Id,
                log.CredentialId,
                log.AccessedAtUtc,
                (int)log.ActorType,
                log.ActorId,
                log.IpAddress,
                log.UserAgent,
                (int)log.AccessMethod,
                (int)log.Outcome,
                orgId);
        }

        using var bulk = new SqlBulkCopy(connection)
        {
            DestinationTableName = "CredentialAccessLogs",
            BatchSize = 1000,
        };
        foreach (DataColumn column in table.Columns)
            bulk.ColumnMappings.Add(column.ColumnName, column.ColumnName);

        await bulk.WriteToServerAsync(table, cancellationToken).ConfigureAwait(false);
    }

    private Guid ResolveOrganizationId(CredentialAccessLog log)
    {
        var entry = _context.Entry(log);
        var orgProp = entry.Property<Guid>("OrganizationId");
        return orgProp.CurrentValue;
    }
}
