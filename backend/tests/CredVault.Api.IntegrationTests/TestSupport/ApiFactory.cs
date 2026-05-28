using System.Security.Cryptography;
using CredVault.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;

namespace CredVault.Api.IntegrationTests.TestSupport;

/// <summary>Spins up a real SQL Server 2022 container and a WebApplicationFactory pointed at it.</summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _sql = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    public string ConnectionString { get; private set; } = string.Empty;
    public string MasterKeyBase64 { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        await _sql.StartAsync();
        ConnectionString = _sql.GetConnectionString();
        MasterKeyBase64 = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

        await using var scope = Services.CreateAsyncScope();
        var ctx = scope.ServiceProvider.GetRequiredService<CredVaultDbContext>();
        await ctx.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        await _sql.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = ConnectionString,
                ["Database:UseNativeJsonColumnType"] = "false",
                ["MasterKey:Versions:0:Version"] = "1",
                ["MasterKey:Versions:0:Base64Key"] = MasterKeyBase64,
                ["Jwt:Secret"] = JwtTestIssuer.Secret,
                ["Jwt:Issuer"] = JwtTestIssuer.Issuer,
                ["Jwt:Audience"] = JwtTestIssuer.Audience,
            });
        });
    }

    public HttpClient AuthedClient(string token)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public HttpClient AuthedClient(string token, string stepUp)
    {
        var client = AuthedClient(token);
        client.DefaultRequestHeaders.Add(CredVault.Api.Auth.AuthConstants.StepUpHeader, stepUp);
        return client;
    }
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "Api";
}
