using CredVault.Api.Auth;
using CredVault.Api.Contracts;
using CredVault.Api.Filters;
using CredVault.Api.Lookups;
using CredVault.Domain.Abstractions;
using CredVault.Domain.Credentials;
using CredVault.Domain.Enums;
using CredVault.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CredVault.Api.Endpoints;

/// <summary>Endpoints for the credential aggregate — create, retrieve, rotate, revoke, delete, history.</summary>
public static class CredentialEndpoints
{
    public static IEndpointRouteBuilder MapCredentialEndpoints(this IEndpointRouteBuilder routes)
    {
        // Create: nested under the (project, env, supplier) URL path
        var createGroup = routes.MapGroup("/api/v1/orgs/{orgSlug}/projects/{projectSlug}/environments/{envSlug}/suppliers/{supplierId:guid}/credentials")
            .WithTags("Credentials")
            .RequireAuthorization();
        createGroup.MapPost("", Create)
            .RequireAuthorization(Permissions.WriteCredentials)
            .RequireAuthorization(AuthConstants.StepUpPolicy)
            .AddEndpointFilter<AuditHookFilter>();

        // Read + rotate + revoke + delete on the org-scoped collection
        var group = routes.MapGroup("/api/v1/orgs/{orgSlug}/credentials")
            .WithTags("Credentials")
            .RequireAuthorization();

        group.MapGet("", List).RequireAuthorization(Permissions.ReadMetadata);
        group.MapGet("/{id:guid}", GetMetadata).RequireAuthorization(Permissions.ReadMetadata);

        group.MapGet("/{id:guid}/value", GetValueById)
            .RequireAuthorization(Permissions.ReadValue)
            .WithMetadata(new SafetyNetAllowlistMarker());

        group.MapGet("/by-path/{projectSlug}/{envSlug}/{supplierSlug}/{credSlug}/value", GetValueByPath)
            .RequireAuthorization(Permissions.ReadValue)
            .WithMetadata(new SafetyNetAllowlistMarker());

        group.MapPost("/{id:guid}/rotate", Rotate)
            .RequireAuthorization(Permissions.WriteCredentials)
            .RequireAuthorization(AuthConstants.StepUpPolicy)
            .AddEndpointFilter<AuditHookFilter>();

        group.MapPost("/{id:guid}/revoke", Revoke)
            .RequireAuthorization(Permissions.WriteCredentials)
            .RequireAuthorization(AuthConstants.StepUpPolicy)
            .AddEndpointFilter<AuditHookFilter>();

        group.MapDelete("/{id:guid}", Delete)
            .RequireAuthorization(Permissions.WriteCredentials)
            .RequireAuthorization(AuthConstants.StepUpPolicy)
            .AddEndpointFilter<AuditHookFilter>();

        group.MapGet("/{id:guid}/rotations", ListRotations).RequireAuthorization(Permissions.ReadMetadata);
        group.MapGet("/{id:guid}/access-log", ListAccessLog).RequireAuthorization(Permissions.ReadMetadata);

        routes.MapGet("/api/v1/orgs/{orgSlug}/access-log", ListOrgAccessLog)
            .WithTags("AccessLog")
            .RequireAuthorization(Permissions.ReadMetadata);

        return routes;
    }

    // ─── Create ────────────────────────────────────────────────────────────

    private static async Task<Results<Created<CredentialMetadataDto>, ProblemHttpResult, ValidationProblem>> Create(
        [FromRoute] string orgSlug,
        [FromRoute] string projectSlug,
        [FromRoute] string envSlug,
        [FromRoute] Guid supplierId,
        [FromBody] CreateCredentialRequest request,
        SlugLookup slugs,
        CredVaultDbContext context,
        ICredentialVaultService vault,
        HttpContext http,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var orgId = await slugs.FindOrganizationIdAsync(orgSlug, ct).ConfigureAwait(false);
        if (orgId is null) return ProblemDetailsHelpers.NotFound("Organisation not found.");
        var projectId = await slugs.FindProjectIdAsync(orgId.Value, projectSlug, ct).ConfigureAwait(false);
        if (projectId is null) return ProblemDetailsHelpers.NotFound("Project not found.");
        var environmentId = await slugs.FindEnvironmentIdAsync(projectId.Value, envSlug, ct).ConfigureAwait(false);
        if (environmentId is null) return ProblemDetailsHelpers.NotFound("Environment not found.");

        var supplier = await context.CredentialSuppliers
            .FirstOrDefaultAsync(s => s.Id == supplierId && s.OrganizationId == orgId, ct).ConfigureAwait(false);
        if (supplier is null) return ProblemDetailsHelpers.NotFound("Supplier not found.");
        if (!supplier.IsActive) return ProblemDetailsHelpers.Conflict("Supplier is inactive.");

        if (!CheckPayloadSize(request.Fields, out var sizeError))
            return ProblemDetailsHelpers.BadRequest(sizeError!);

        try
        {
            var credId = await vault.StoreAsync(
                supplier.Id, environmentId.Value, request.Name, Slug.Create(request.Slug),
                request.Fields, request.ExpiresAtUtc, ct).ConfigureAwait(false);

            var credential = await context.Credentials.AsNoTracking().FirstAsync(c => c.Id == credId, ct).ConfigureAwait(false);

            http.Items[AuditHookFilter.OrganizationIdItem] = orgId.Value;
            http.Items[AuditHookFilter.ActionItem] = "credential.created";
            http.Items[AuditHookFilter.TargetTypeItem] = "Credential";
            http.Items[AuditHookFilter.TargetIdItem] = credential.Id.ToString();

            return TypedResults.Created(
                $"/api/v1/orgs/{orgSlug}/credentials/{credential.Id}",
                ToMetadataDto(credential, supplier.SupplierType));
        }
        catch (DomainException ex)
        {
            return ProblemDetailsHelpers.BadRequest(ex.Message);
        }
    }

    // ─── List / Get metadata ───────────────────────────────────────────────

    private static async Task<Results<Ok<IReadOnlyList<CredentialMetadataDto>>, ProblemHttpResult>> List(
        [FromRoute] string orgSlug,
        [FromQuery] string? project,
        [FromQuery] string? environment,
        [FromQuery] Guid? supplierId,
        SlugLookup slugs,
        CredVaultDbContext context,
        CancellationToken ct)
    {
        var orgId = await slugs.FindOrganizationIdAsync(orgSlug, ct).ConfigureAwait(false);
        if (orgId is null) return ProblemDetailsHelpers.NotFound("Organisation not found.");

        var query =
            from c in context.Credentials.AsNoTracking()
            join s in context.CredentialSuppliers.AsNoTracking() on c.SupplierId equals s.Id
            where s.OrganizationId == orgId
            select new { Credential = c, Supplier = s };

        if (supplierId is { } supId)
            query = query.Where(x => x.Credential.SupplierId == supId);
        if (!string.IsNullOrWhiteSpace(project))
        {
            var projectId = await slugs.FindProjectIdAsync(orgId.Value, project, ct).ConfigureAwait(false);
            if (projectId is null) return TypedResults.Ok<IReadOnlyList<CredentialMetadataDto>>([]);
            if (!string.IsNullOrWhiteSpace(environment))
            {
                var envId = await slugs.FindEnvironmentIdAsync(projectId.Value, environment, ct).ConfigureAwait(false);
                if (envId is null) return TypedResults.Ok<IReadOnlyList<CredentialMetadataDto>>([]);
                query = query.Where(x => x.Credential.EnvironmentId == envId);
            }
            else
            {
                var envIds = await context.Environments.AsNoTracking()
                    .Where(e => e.ProjectId == projectId)
                    .Select(e => e.Id)
                    .ToListAsync(ct).ConfigureAwait(false);
                query = query.Where(x => envIds.Contains(x.Credential.EnvironmentId));
            }
        }

        var rows = await query
            .OrderByDescending(x => x.Credential.CreatedAtUtc)
            .Take(500)
            .ToListAsync(ct).ConfigureAwait(false);

        return TypedResults.Ok<IReadOnlyList<CredentialMetadataDto>>(
            rows.Select(x => ToMetadataDto(x.Credential, x.Supplier.SupplierType)).ToList());
    }

    private static async Task<Results<Ok<CredentialMetadataDto>, ProblemHttpResult>> GetMetadata(
        [FromRoute] string orgSlug,
        [FromRoute] Guid id,
        SlugLookup slugs,
        CredVaultDbContext context,
        CancellationToken ct)
    {
        var orgId = await slugs.FindOrganizationIdAsync(orgSlug, ct).ConfigureAwait(false);
        if (orgId is null) return ProblemDetailsHelpers.NotFound("Organisation not found.");

        var row = await (
            from c in context.Credentials.AsNoTracking()
            join s in context.CredentialSuppliers.AsNoTracking() on c.SupplierId equals s.Id
            where c.Id == id && s.OrganizationId == orgId
            select new { Credential = c, Supplier = s }).FirstOrDefaultAsync(ct).ConfigureAwait(false);
        if (row is null) return ProblemDetailsHelpers.NotFound("Credential not found.");

        return TypedResults.Ok(ToMetadataDto(row.Credential, row.Supplier.SupplierType));
    }

    // ─── Value (decrypt) ───────────────────────────────────────────────────

    private static async Task<Results<Ok<CredentialValueResponse>, ProblemHttpResult>> GetValueById(
        [FromRoute] string orgSlug,
        [FromRoute] Guid id,
        SlugLookup slugs,
        CredVaultDbContext context,
        ICredentialVaultService vault,
        ICurrentUser currentUser,
        HttpContext http,
        CancellationToken ct)
    {
        var orgId = await slugs.FindOrganizationIdAsync(orgSlug, ct).ConfigureAwait(false);
        if (orgId is null) return ProblemDetailsHelpers.NotFound("Organisation not found.");
        return await RetrieveValueAsync(id, orgId.Value, context, vault, currentUser, http, ct).ConfigureAwait(false);
    }

    private static async Task<Results<Ok<CredentialValueResponse>, ProblemHttpResult>> GetValueByPath(
        [FromRoute] string orgSlug,
        [FromRoute] string projectSlug,
        [FromRoute] string envSlug,
        [FromRoute] string supplierSlug,
        [FromRoute] string credSlug,
        SlugLookup slugs,
        CredVaultDbContext context,
        ICredentialVaultService vault,
        ICurrentUser currentUser,
        HttpContext http,
        CancellationToken ct)
    {
        var orgId = await slugs.FindOrganizationIdAsync(orgSlug, ct).ConfigureAwait(false);
        if (orgId is null) return ProblemDetailsHelpers.NotFound("Organisation not found.");
        var projectId = await slugs.FindProjectIdAsync(orgId.Value, projectSlug, ct).ConfigureAwait(false);
        if (projectId is null) return ProblemDetailsHelpers.NotFound("Project not found.");
        var envId = await slugs.FindEnvironmentIdAsync(projectId.Value, envSlug, ct).ConfigureAwait(false);
        if (envId is null) return ProblemDetailsHelpers.NotFound("Environment not found.");
        var supplierId = await slugs.FindSupplierIdAsync(orgId.Value, supplierSlug, ct).ConfigureAwait(false);
        if (supplierId is null) return ProblemDetailsHelpers.NotFound("Supplier not found.");
        var credId = await slugs.FindCredentialIdAsync(envId.Value, supplierId.Value, credSlug, ct).ConfigureAwait(false);
        if (credId is null) return ProblemDetailsHelpers.NotFound("Credential not found.");

        return await RetrieveValueAsync(credId.Value, orgId.Value, context, vault, currentUser, http, ct).ConfigureAwait(false);
    }

    private static async Task<Results<Ok<CredentialValueResponse>, ProblemHttpResult>> RetrieveValueAsync(
        Guid credId,
        Guid orgId,
        CredVaultDbContext context,
        ICredentialVaultService vault,
        ICurrentUser currentUser,
        HttpContext http,
        CancellationToken ct)
    {
        // Production environment + User actor → step-up required.
        var envType = await (
            from c in context.Credentials.AsNoTracking()
            join e in context.Environments.AsNoTracking() on c.EnvironmentId equals e.Id
            where c.Id == credId
            select (EnvironmentType?)e.Type).FirstOrDefaultAsync(ct).ConfigureAwait(false);
        if (envType is null) return ProblemDetailsHelpers.NotFound("Credential not found.");

        if (envType == EnvironmentType.Production && currentUser.ActorType == ActorType.User)
        {
            var http2 = http;
            var requiresStepUp = !await HasValidStepUpAsync(http2).ConfigureAwait(false);
            if (requiresStepUp)
                return ProblemDetailsHelpers.StepUpRequired();
        }

        var access = new CredentialAccessContext(
            currentUser.ActorType, currentUser.ActorId, currentUser.IpAddress, currentUser.UserAgent, AccessMethod.UI);

        try
        {
            var fields = await vault.RetrieveAsync(credId, access, ct).ConfigureAwait(false);

            // Read the access-log row that the vault service just inserted.
            var log = await context.CredentialAccessLogs.AsNoTracking()
                .Where(l => l.CredentialId == credId && l.ActorId == currentUser.ActorId && l.Outcome == AccessOutcome.Success)
                .OrderByDescending(l => l.AccessedAtUtc)
                .FirstAsync(ct).ConfigureAwait(false);

            var accessDto = new CredentialAccessDto(
                log.Id, log.CredentialId, log.AccessedAtUtc, log.ActorType, log.ActorId,
                log.IpAddress, log.UserAgent, log.AccessMethod, log.Outcome);

            return TypedResults.Ok(new CredentialValueResponse(fields, accessDto));
        }
        catch (DomainException ex) when (ex.Message.Contains("Rate limit", StringComparison.OrdinalIgnoreCase))
        {
            return TypedResults.Problem(title: "Too many requests", statusCode: 429, detail: ex.Message);
        }
        catch (DomainException ex)
        {
            return ProblemDetailsHelpers.BadRequest(ex.Message);
        }
    }

    // ─── Rotate / Revoke / Delete ──────────────────────────────────────────

    private static async Task<Results<Ok<CredentialMetadataDto>, ProblemHttpResult>> Rotate(
        [FromRoute] string orgSlug,
        [FromRoute] Guid id,
        [FromBody] RotateCredentialRequest request,
        SlugLookup slugs,
        CredVaultDbContext context,
        ICredentialVaultService vault,
        HttpContext http,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var orgId = await slugs.FindOrganizationIdAsync(orgSlug, ct).ConfigureAwait(false);
        if (orgId is null) return ProblemDetailsHelpers.NotFound("Organisation not found.");

        if (!CheckPayloadSize(request.Fields, out var sizeError))
            return ProblemDetailsHelpers.BadRequest(sizeError!);

        try
        {
            await vault.RotateAsync(id, request.Fields, request.ExpiresAtUtc, request.Reason, ct).ConfigureAwait(false);

            var row = await (
                from c in context.Credentials.AsNoTracking()
                join s in context.CredentialSuppliers.AsNoTracking() on c.SupplierId equals s.Id
                where c.Id == id && s.OrganizationId == orgId
                select new { Credential = c, Supplier = s }).FirstAsync(ct).ConfigureAwait(false);

            http.Items[AuditHookFilter.OrganizationIdItem] = orgId.Value;
            http.Items[AuditHookFilter.ActionItem] = "credential.rotated";
            http.Items[AuditHookFilter.TargetTypeItem] = "Credential";
            http.Items[AuditHookFilter.TargetIdItem] = id.ToString();

            return TypedResults.Ok(ToMetadataDto(row.Credential, row.Supplier.SupplierType));
        }
        catch (DomainException ex)
        {
            return ProblemDetailsHelpers.BadRequest(ex.Message);
        }
    }

    private static async Task<Results<Ok<CredentialMetadataDto>, ProblemHttpResult>> Revoke(
        [FromRoute] string orgSlug,
        [FromRoute] Guid id,
        SlugLookup slugs,
        CredVaultDbContext context,
        IUnitOfWork uow,
        IDateTimeProvider clock,
        ICurrentUser currentUser,
        HttpContext http,
        CancellationToken ct)
    {
        var orgId = await slugs.FindOrganizationIdAsync(orgSlug, ct).ConfigureAwait(false);
        if (orgId is null) return ProblemDetailsHelpers.NotFound("Organisation not found.");

        var row = await (
            from c in context.Credentials
            join s in context.CredentialSuppliers on c.SupplierId equals s.Id
            where c.Id == id && s.OrganizationId == orgId
            select new { Credential = c, Supplier = s }).FirstOrDefaultAsync(ct).ConfigureAwait(false);
        if (row is null) return ProblemDetailsHelpers.NotFound("Credential not found.");

        try
        {
            row.Credential.Revoke(currentUser.ActorId, clock);
            await uow.SaveChangesAsync(ct).ConfigureAwait(false);

            http.Items[AuditHookFilter.OrganizationIdItem] = orgId.Value;
            http.Items[AuditHookFilter.ActionItem] = "credential.revoked";
            http.Items[AuditHookFilter.TargetTypeItem] = "Credential";
            http.Items[AuditHookFilter.TargetIdItem] = id.ToString();

            return TypedResults.Ok(ToMetadataDto(row.Credential, row.Supplier.SupplierType));
        }
        catch (DomainException ex)
        {
            return ProblemDetailsHelpers.BadRequest(ex.Message);
        }
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> Delete(
        [FromRoute] string orgSlug,
        [FromRoute] Guid id,
        SlugLookup slugs,
        CredVaultDbContext context,
        IUnitOfWork uow,
        HttpContext http,
        CancellationToken ct)
    {
        var orgId = await slugs.FindOrganizationIdAsync(orgSlug, ct).ConfigureAwait(false);
        if (orgId is null) return ProblemDetailsHelpers.NotFound("Organisation not found.");

        var credential = await (
            from c in context.Credentials
            join s in context.CredentialSuppliers on c.SupplierId equals s.Id
            where c.Id == id && s.OrganizationId == orgId
            select c).FirstOrDefaultAsync(ct).ConfigureAwait(false);
        if (credential is null) return ProblemDetailsHelpers.NotFound("Credential not found.");

        context.Credentials.Remove(credential);
        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        http.Items[AuditHookFilter.OrganizationIdItem] = orgId.Value;
        http.Items[AuditHookFilter.ActionItem] = "credential.deleted";
        http.Items[AuditHookFilter.TargetTypeItem] = "Credential";
        http.Items[AuditHookFilter.TargetIdItem] = id.ToString();

        return TypedResults.NoContent();
    }

    // ─── History endpoints ─────────────────────────────────────────────────

    private static async Task<Results<Ok<IReadOnlyList<CredentialRotationDto>>, ProblemHttpResult>> ListRotations(
        [FromRoute] string orgSlug,
        [FromRoute] Guid id,
        SlugLookup slugs,
        CredVaultDbContext context,
        CancellationToken ct)
    {
        var orgId = await slugs.FindOrganizationIdAsync(orgSlug, ct).ConfigureAwait(false);
        if (orgId is null) return ProblemDetailsHelpers.NotFound("Organisation not found.");

        var credential = await context.Credentials.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, ct).ConfigureAwait(false);
        if (credential is null) return ProblemDetailsHelpers.NotFound("Credential not found.");

        var rows = await context.CredentialRotations.AsNoTracking()
            .Where(r => r.CredentialId == id)
            .OrderByDescending(r => r.RotatedAtUtc)
            .ToListAsync(ct).ConfigureAwait(false);

        // NEVER include PreviousEncryptedPayload / WrappedDataKey / Nonce / AuthTag in the response.
        return TypedResults.Ok<IReadOnlyList<CredentialRotationDto>>(
            rows.Select(r => new CredentialRotationDto(r.Id, r.CredentialId, r.RotatedAtUtc, r.RotatedByUserId, r.PreviousKekVersion, r.Reason)).ToList());
    }

    private static async Task<Results<Ok<CursorPage<CredentialAccessDto>>, ProblemHttpResult>> ListAccessLog(
        [FromRoute] string orgSlug,
        [FromRoute] Guid id,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] ActorType? actorType,
        [FromQuery] int? limit,
        [FromQuery] string? cursor,
        SlugLookup slugs,
        CredVaultDbContext context,
        CancellationToken ct)
    {
        var orgId = await slugs.FindOrganizationIdAsync(orgSlug, ct).ConfigureAwait(false);
        if (orgId is null) return ProblemDetailsHelpers.NotFound("Organisation not found.");

        var take = Math.Clamp(limit ?? 50, 1, 200);
        var cursorTs = ParseCursor(cursor);

        var query = context.CredentialAccessLogs.AsNoTracking()
            .Where(l => l.CredentialId == id);

        if (from is { } f) query = query.Where(l => l.AccessedAtUtc >= f);
        if (to is { } t) query = query.Where(l => l.AccessedAtUtc <= t);
        if (actorType is { } a) query = query.Where(l => l.ActorType == a);
        if (cursorTs is { } cts) query = query.Where(l => l.AccessedAtUtc < cts);

        var rows = await query.OrderByDescending(l => l.AccessedAtUtc).Take(take + 1).ToListAsync(ct).ConfigureAwait(false);

        var hasMore = rows.Count > take;
        if (hasMore) rows.RemoveAt(rows.Count - 1);

        var dtos = rows.Select(l => new CredentialAccessDto(
            l.Id, l.CredentialId, l.AccessedAtUtc, l.ActorType, l.ActorId,
            l.IpAddress, l.UserAgent, l.AccessMethod, l.Outcome)).ToList();

        var nextCursor = hasMore && rows.Count > 0
            ? rows[^1].AccessedAtUtc.ToString("O", System.Globalization.CultureInfo.InvariantCulture)
            : null;

        return TypedResults.Ok(new CursorPage<CredentialAccessDto>(dtos, nextCursor));
    }

    private static async Task<Results<Ok<CursorPage<CredentialAccessDto>>, ProblemHttpResult>> ListOrgAccessLog(
        [FromRoute] string orgSlug,
        [FromQuery] Guid? credentialId,
        [FromQuery] Guid? actorId,
        [FromQuery] int? limit,
        [FromQuery] string? cursor,
        SlugLookup slugs,
        CredVaultDbContext context,
        CancellationToken ct)
    {
        var orgId = await slugs.FindOrganizationIdAsync(orgSlug, ct).ConfigureAwait(false);
        if (orgId is null) return ProblemDetailsHelpers.NotFound("Organisation not found.");

        var take = Math.Clamp(limit ?? 50, 1, 200);
        var cursorTs = ParseCursor(cursor);

        var query = from l in context.CredentialAccessLogs.AsNoTracking()
                    join c in context.Credentials.AsNoTracking() on l.CredentialId equals c.Id
                    join s in context.CredentialSuppliers.AsNoTracking() on c.SupplierId equals s.Id
                    where s.OrganizationId == orgId
                    select l;
        if (credentialId is { } cid) query = query.Where(l => l.CredentialId == cid);
        if (actorId is { } aid) query = query.Where(l => l.ActorId == aid);
        if (cursorTs is { } cts) query = query.Where(l => l.AccessedAtUtc < cts);

        var rows = await query.OrderByDescending(l => l.AccessedAtUtc).Take(take + 1).ToListAsync(ct).ConfigureAwait(false);
        var hasMore = rows.Count > take;
        if (hasMore) rows.RemoveAt(rows.Count - 1);

        var dtos = rows.Select(l => new CredentialAccessDto(
            l.Id, l.CredentialId, l.AccessedAtUtc, l.ActorType, l.ActorId,
            l.IpAddress, l.UserAgent, l.AccessMethod, l.Outcome)).ToList();

        var nextCursor = hasMore && rows.Count > 0
            ? rows[^1].AccessedAtUtc.ToString("O", System.Globalization.CultureInfo.InvariantCulture)
            : null;

        return TypedResults.Ok(new CursorPage<CredentialAccessDto>(dtos, nextCursor));
    }

    // ─── Helpers ───────────────────────────────────────────────────────────

    internal const int MaxFieldValueBytes = 4 * 1024;
    internal const int MaxPayloadBytes = 32 * 1024;

    internal static bool CheckPayloadSize(IReadOnlyDictionary<string, string> fields, out string? error)
    {
        var total = 0;
        foreach (var (k, v) in fields)
        {
            var valueBytes = System.Text.Encoding.UTF8.GetByteCount(v ?? string.Empty);
            if (valueBytes > MaxFieldValueBytes)
            {
                error = $"Field '{k}' exceeds the {MaxFieldValueBytes}-byte limit.";
                return false;
            }
            total += System.Text.Encoding.UTF8.GetByteCount(k) + valueBytes;
        }
        if (total > MaxPayloadBytes)
        {
            error = $"Payload exceeds the {MaxPayloadBytes}-byte limit.";
            return false;
        }
        error = null;
        return true;
    }

    internal static async Task<bool> HasValidStepUpAsync(HttpContext http)
    {
        var policy = http.RequestServices.GetRequiredService<Microsoft.AspNetCore.Authorization.IAuthorizationService>();
        var result = await policy.AuthorizeAsync(http.User, null, AuthConstants.StepUpPolicy).ConfigureAwait(false);
        return result.Succeeded;
    }

    private static DateTime? ParseCursor(string? cursor) =>
        DateTime.TryParse(cursor, System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal,
            out var ts) ? ts : null;

    private static CredentialMetadataDto ToMetadataDto(Credential c, SupplierType supplierType) =>
        new(c.Id, c.SupplierId, supplierType, c.EnvironmentId, c.Name, c.Slug.Value, c.MaskedPreview,
            c.CredentialSchemaVersion, c.KekVersion, c.CreatedAtUtc, c.RotatedAtUtc, c.ExpiresAtUtc,
            c.LastAccessedAtUtc, c.AccessCount, c.IsRevoked, c.RevokedAtUtc);
}
