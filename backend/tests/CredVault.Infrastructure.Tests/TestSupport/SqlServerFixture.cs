using CredVault.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Testcontainers.MsSql;

namespace CredVault.Infrastructure.Tests.TestSupport;

/// <summary>
/// Spins up a real SQL Server 2022 container, applies the EF migration once, and exposes a connection
/// string for tests. Shared across the integration collection so we pay the container start cost once.
/// </summary>
public sealed class SqlServerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    /// <summary>Connection string into the running container.</summary>
    public string ConnectionString { get; private set; } = string.Empty;

    /// <summary>Factory for fresh contexts inside a test.</summary>
    public CredVaultDbContext CreateContext(Action<string>? sqlSink = null)
    {
        var builder = new DbContextOptionsBuilder<CredVaultDbContext>()
            .UseSqlServer(ConnectionString);
        if (sqlSink is not null)
            builder.LogTo(sqlSink, Microsoft.Extensions.Logging.LogLevel.Information);
        var contextOptions = Options.Create(new CredVaultDbContextOptions { UseNativeJsonColumnType = false });
        return new CredVaultDbContext(builder.Options, contextOptions);
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();
        await using var ctx = CreateContext();
        await ctx.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class SqlServerCollection : ICollectionFixture<SqlServerFixture>
{
    public const string Name = "SqlServer";
}
