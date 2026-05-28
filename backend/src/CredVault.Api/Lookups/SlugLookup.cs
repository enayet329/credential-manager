using CredVault.Domain.Enums;
using CredVault.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CredVault.Api.Lookups;

/// <summary>
/// Resolves slug-based URL segments to entity ids. Lightweight wrapper around the DbContext so
/// endpoints stay terse. Comparisons go through the configured Slug value converter, so we parse the
/// URL segment into a <see cref="Slug"/> first and return null on malformed input rather than 500.
/// </summary>
public sealed class SlugLookup
{
    private readonly CredVaultDbContext _context;

    public SlugLookup(CredVaultDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    private static Slug? TryCreate(string raw)
    {
        try { return Slug.Create(raw); }
        catch (DomainException) { return null; }
    }

    /// <summary>Returns the org's id, or null if no match.</summary>
    public async Task<Guid?> FindOrganizationIdAsync(string orgSlug, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(orgSlug);
        var slug = TryCreate(orgSlug);
        if (slug is null) return null;
        return await _context.Organizations
            .Where(o => o.Slug == slug)
            .Select(o => (Guid?)o.Id)
            .FirstOrDefaultAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Returns the project's id (scoped to org), or null.</summary>
    public async Task<Guid?> FindProjectIdAsync(Guid orgId, string projectSlug, CancellationToken ct)
    {
        var slug = TryCreate(projectSlug);
        if (slug is null) return null;
        return await _context.Projects
            .Where(p => p.OrganizationId == orgId && p.Slug == slug)
            .Select(p => (Guid?)p.Id)
            .FirstOrDefaultAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Returns the environment's id (scoped to project), or null.</summary>
    public async Task<Guid?> FindEnvironmentIdAsync(Guid projectId, string envSlug, CancellationToken ct)
    {
        var slug = TryCreate(envSlug);
        if (slug is null) return null;
        return await _context.Environments
            .Where(e => e.ProjectId == projectId && e.Slug == slug)
            .Select(e => (Guid?)e.Id)
            .FirstOrDefaultAsync(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Returns the supplier's id (scoped to org). The URL segment is the kebab-case form of
    /// <see cref="SupplierType"/>; we look up via the typed column for predictability.
    /// </summary>
    public async Task<Guid?> FindSupplierIdAsync(Guid orgId, string supplierSlug, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(supplierSlug);
        if (!TryParseSupplier(supplierSlug, out var type))
            return null;

        return await _context.CredentialSuppliers
            .Where(s => s.OrganizationId == orgId && s.SupplierType == type)
            .OrderByDescending(s => s.CreatedAtUtc)
            .Select(s => (Guid?)s.Id)
            .FirstOrDefaultAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Looks up a credential by (env, supplier, slug).</summary>
    public async Task<Guid?> FindCredentialIdAsync(Guid environmentId, Guid supplierId, string credSlug, CancellationToken ct)
    {
        var slug = TryCreate(credSlug);
        if (slug is null) return null;
        return await _context.Credentials
            .Where(c => c.EnvironmentId == environmentId && c.SupplierId == supplierId && c.Slug == slug)
            .Select(c => (Guid?)c.Id)
            .FirstOrDefaultAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Parses the URL slug for a supplier into its enum value. Accepts kebab-case and lowercased forms.</summary>
    public static bool TryParseSupplier(string slug, out SupplierType type)
    {
        var compact = slug.Replace("-", string.Empty, StringComparison.Ordinal);
        return Enum.TryParse(compact, ignoreCase: true, out type);
    }

    /// <summary>Returns the kebab-case URL slug for a <see cref="SupplierType"/>.</summary>
    public static string ToSlug(SupplierType type)
    {
        var name = type.ToString();
        var result = new System.Text.StringBuilder(name.Length + 4);
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (char.IsUpper(c) && i > 0)
                result.Append('-');
            result.Append(char.ToLowerInvariant(c));
        }
        return result.ToString();
    }
}
