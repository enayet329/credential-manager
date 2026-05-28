using CredVault.Domain.Abstractions;
using CredVault.Domain.Credentials;
using CredVault.Domain.Enums;
using CredVault.Domain.Organizations;
using CredVault.Domain.Projects;
using CredVault.Domain.Suppliers;
using CredVault.Domain.Users;
using CredVault.Domain.ValueObjects;
using CredVault.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProjectEnv = CredVault.Domain.Projects.Environment;

namespace CredVault.Api.Seeding;

/// <summary>
/// One-shot seeder for an empty database. Idempotent — re-running it after the first seed is a no-op.
/// Invoked by the <c>seed-initial-data</c> CLI command in <c>Program.cs</c>.
/// </summary>
public sealed class InitialDataSeeder
{
    private static readonly Action<ILogger, string, string, Exception?> LogSeeded =
        LoggerMessage.Define<string, string>(
            LogLevel.Information,
            new EventId(4001, nameof(InitialDataSeeder)),
            "Seeded initial data. Org slug: {OrgSlug}. Admin email: {AdminEmail}");

    private static readonly Action<ILogger, Exception?> LogSkipped =
        LoggerMessage.Define(
            LogLevel.Information,
            new EventId(4002, nameof(InitialDataSeeder)),
            "Seed skipped — an organization already exists.");

    private readonly CredVaultDbContext _context;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<InitialDataSeeder> _logger;

    public InitialDataSeeder(CredVaultDbContext context, IDateTimeProvider clock, ILogger<InitialDataSeeder> logger)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(logger);
        _context = context;
        _clock = clock;
        _logger = logger;
    }

    public sealed record SeedResult(
        bool Created,
        Guid OrganizationId,
        string OrganizationSlug,
        Guid AdminUserId,
        string AdminEmail,
        string? AdminPasswordPlaintext);

    public async Task<SeedResult> SeedAsync(
        string orgName,
        string orgSlug,
        string adminEmail,
        string? adminPasswordOverride,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(orgName);
        ArgumentNullException.ThrowIfNull(orgSlug);
        ArgumentNullException.ThrowIfNull(adminEmail);

        if (await _context.Organizations.AsNoTracking().AnyAsync(ct).ConfigureAwait(false))
        {
            LogSkipped(_logger, null);
            var existing = await _context.Organizations.AsNoTracking().FirstAsync(ct).ConfigureAwait(false);
            var existingAdmin = await _context.Users.AsNoTracking().FirstOrDefaultAsync(ct).ConfigureAwait(false);
            return new SeedResult(
                Created: false,
                OrganizationId: existing.Id,
                OrganizationSlug: existing.Slug.Value,
                AdminUserId: existingAdmin?.Id ?? Guid.Empty,
                AdminEmail: existingAdmin?.Email.Value ?? "",
                AdminPasswordPlaintext: null);
        }

        // Build an admin password if the caller didn't supply one.
        var plaintextPassword = adminPasswordOverride ?? GeneratePassword();
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(plaintextPassword, workFactor: 12);

        var org = Organization.Create(orgName, Slug.Create(orgSlug), _clock);
        var admin = User.Register(Email.Create(adminEmail), passwordHash, _clock);
        admin.ConfirmEmail();
        org.AddMember(admin.Id, OrganizationRole.Owner, _clock);

        var mainProject = Project.Create(org.Id, "Main", Slug.Create("main"), "Default project created at seed time.", _clock);
        var devEnv = ProjectEnv.Create(mainProject.Id, "Development", Slug.Create("dev"), EnvironmentType.Development, _clock);
        var stagingEnv = ProjectEnv.Create(mainProject.Id, "Staging", Slug.Create("staging"), EnvironmentType.Staging, _clock);
        var prodEnv = ProjectEnv.Create(mainProject.Id, "Production", Slug.Create("prod"), EnvironmentType.Production, _clock);

        var suppliers = new[]
        {
            CredentialSupplier.Create(org.Id, SupplierType.OpenAI, "OpenAI", _clock),
            CredentialSupplier.Create(org.Id, SupplierType.Anthropic, "Anthropic", _clock),
            CredentialSupplier.Create(org.Id, SupplierType.Stripe, "Stripe", _clock),
            CredentialSupplier.Create(org.Id, SupplierType.GitHub, "GitHub", _clock),
            CredentialSupplier.Create(org.Id, SupplierType.Postgres, "Postgres", _clock),
        };

        _context.Organizations.Add(org);
        _context.Users.Add(admin);
        _context.Projects.Add(mainProject);
        _context.Environments.AddRange(devEnv, stagingEnv, prodEnv);
        _context.CredentialSuppliers.AddRange(suppliers);

        await _context.SaveChangesAsync(ct).ConfigureAwait(false);
        LogSeeded(_logger, org.Slug.Value, admin.Email.Value, null);

        return new SeedResult(
            Created: true,
            OrganizationId: org.Id,
            OrganizationSlug: org.Slug.Value,
            AdminUserId: admin.Id,
            AdminEmail: admin.Email.Value,
            AdminPasswordPlaintext: plaintextPassword);
    }

    private static string GeneratePassword()
    {
        // 16 url-safe characters from cryptographic RNG, biased away from look-alike chars.
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";
        Span<byte> bytes = stackalloc byte[16];
        System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
        Span<char> chars = stackalloc char[16];
        for (var i = 0; i < bytes.Length; i++)
            chars[i] = alphabet[bytes[i] % alphabet.Length];
        return new string(chars);
    }
}
