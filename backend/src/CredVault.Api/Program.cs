using CredVault.Api;
using CredVault.Api.Seeding;
using CredVault.Infrastructure;
using CredVault.Infrastructure.Cryptography;
using CredVault.Infrastructure.Logging;
using CredVault.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

// ─── One-shot CLI subcommands ──────────────────────────────────────────────

if (args.Length > 0 && string.Equals(args[0], "generate-master-key", StringComparison.OrdinalIgnoreCase))
{
    Console.WriteLine(MasterKeyGenerator.GenerateBase64());
    return 0;
}

if (args.Length > 0 && string.Equals(args[0], "seed-initial-data", StringComparison.OrdinalIgnoreCase))
{
    return await RunSeedAsync(args);
}

if (args.Length > 0 && string.Equals(args[0], "migrate", StringComparison.OrdinalIgnoreCase))
{
    return await RunMigrateAsync(args);
}

if (args.Length > 0 && string.Equals(args[0], "send-test-email", StringComparison.OrdinalIgnoreCase))
{
    return await RunSendTestEmailAsync(args);
}

// ─── Web app ───────────────────────────────────────────────────────────────

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, _, configuration) =>
{
    configuration
        .Enrich.FromLogContext()
        .Enrich.With(new SensitivePropertyMasker())
        .WriteTo.Console(formatter: new Serilog.Formatting.Json.JsonFormatter())
        .ReadFrom.Configuration(context.Configuration);
});

builder.Services.AddCredVaultInfrastructure(builder.Configuration);
builder.Services.AddCredVaultApi(builder.Configuration);
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseStatusCodePages();

app.MapOpenApi();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapCredVaultEndpoints();

app.Run();
return 0;

// ─── Seed implementation ───────────────────────────────────────────────────

static async Task<int> RunSeedAsync(string[] args)
{
    var seedBuilder = WebApplication.CreateBuilder(args);
    seedBuilder.Services.AddCredVaultInfrastructure(seedBuilder.Configuration);
    seedBuilder.Services.AddTransient<InitialDataSeeder>();
    var seedApp = seedBuilder.Build();

    var orgName = GetArg(args, "--org-name") ?? "CredVault HQ";
    var orgSlug = GetArg(args, "--org-slug") ?? "credvault";
    var adminEmail = GetArg(args, "--admin-email") ?? "admin@credvault.local";
    var adminPasswordOverride = GetArg(args, "--admin-password");
    var applyMigrations = !args.Contains("--skip-migrate", StringComparer.OrdinalIgnoreCase);

    using var scope = seedApp.Services.CreateScope();
    var sp = scope.ServiceProvider;
    var logger = sp.GetRequiredService<ILogger<InitialDataSeeder>>();

    if (applyMigrations)
    {
        var ctx = sp.GetRequiredService<CredVaultDbContext>();
        Console.WriteLine("Applying EF Core migrations…");
        await ctx.Database.MigrateAsync();
    }

    var seeder = sp.GetRequiredService<InitialDataSeeder>();
    var result = await seeder.SeedAsync(orgName, orgSlug, adminEmail, adminPasswordOverride, CancellationToken.None);

    Console.WriteLine();
    Console.WriteLine("───── CredVault seed result ─────");
    Console.WriteLine($"created:           {result.Created}");
    Console.WriteLine($"organization id:   {result.OrganizationId}");
    Console.WriteLine($"organization slug: {result.OrganizationSlug}");
    Console.WriteLine($"admin user id:     {result.AdminUserId}");
    Console.WriteLine($"admin email:       {result.AdminEmail}");
    if (result.AdminPasswordPlaintext is not null)
    {
        Console.WriteLine($"admin password:    {result.AdminPasswordPlaintext}");
        Console.WriteLine("(STORE THIS — it is shown once and never again.)");
    }
    else
    {
        Console.WriteLine("admin password:    <seed skipped — org already existed>");
    }
    Console.WriteLine("─────────────────────────────────");
    return 0;
}

static async Task<int> RunMigrateAsync(string[] args)
{
    var b = WebApplication.CreateBuilder(args);
    b.Services.AddCredVaultInfrastructure(b.Configuration);
    var app = b.Build();
    using var scope = app.Services.CreateScope();
    var ctx = scope.ServiceProvider.GetRequiredService<CredVaultDbContext>();
    Console.WriteLine("Applying EF Core migrations…");
    await ctx.Database.MigrateAsync();
    Console.WriteLine("Migrations applied.");
    return 0;
}

static async Task<int> RunSendTestEmailAsync(string[] args)
{
    var to = GetArg(args, "--to") ?? GetArg(args, "-t");
    if (string.IsNullOrWhiteSpace(to))
    {
        Console.Error.WriteLine("Usage: dotnet run -- send-test-email --to recipient@example.com");
        return 2;
    }

    var b = WebApplication.CreateBuilder(args);
    b.Services.AddCredVaultInfrastructure(b.Configuration);
    b.Services.AddCredVaultApi(b.Configuration);
    var app = b.Build();
    using var scope = app.Services.CreateScope();
    var sender = scope.ServiceProvider.GetRequiredService<CredVault.Api.Auth.IEmailSender>();
    Console.WriteLine($"Sending test email to {to} via the configured IEmailSender ({sender.GetType().Name})…");
    await sender.SendAsync(to,
        "CredVault test email",
        "If you can read this, your SMTP configuration is working end-to-end.");
    Console.WriteLine("Sent.");
    return 0;
}

static string? GetArg(string[] args, string name)
{
    for (var i = 0; i < args.Length - 1; i++)
        if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
            return args[i + 1];
    return null;
}

public partial class Program;
