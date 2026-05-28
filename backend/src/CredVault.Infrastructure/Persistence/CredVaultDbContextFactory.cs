using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Options;

namespace CredVault.Infrastructure.Persistence;

/// <summary>
/// Design-time factory used by <c>dotnet ef migrations</c>. Reads a connection string from the
/// <c>CREDVAULT_DESIGNTIME_CONNECTION</c> env var if set; otherwise uses a placeholder so the model
/// can still be built for migration generation.
/// </summary>
public sealed class CredVaultDbContextFactory : IDesignTimeDbContextFactory<CredVaultDbContext>
{
    /// <inheritdoc/>
    public CredVaultDbContext CreateDbContext(string[] args)
    {
        var connectionString = System.Environment.GetEnvironmentVariable("CREDVAULT_DESIGNTIME_CONNECTION")
            ?? "Server=localhost,1433;Database=CredVault.DesignTime;User Id=sa;Password=Local_Dev_Passw0rd!;TrustServerCertificate=True;Encrypt=False;";

        var optionsBuilder = new DbContextOptionsBuilder<CredVaultDbContext>()
            .UseSqlServer(connectionString, sql => sql.MigrationsAssembly(typeof(CredVaultDbContext).Assembly.FullName));

        var credVaultOptions = Options.Create(new CredVaultDbContextOptions
        {
            UseNativeJsonColumnType = false,
        });

        return new CredVaultDbContext(optionsBuilder.Options, credVaultOptions);
    }
}
