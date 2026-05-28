using CredVault.Api.Auth;
using CredVault.Api.Contracts;
using CredVault.Api.Filters;
using CredVault.Api.Lookups;
using CredVault.Domain.Abstractions;
using CredVault.Domain.Suppliers;
using CredVault.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CredVault.Api.Endpoints;

/// <summary>Credential-supplier CRUD endpoints.</summary>
public static class SupplierEndpoints
{
    public static IEndpointRouteBuilder MapSupplierEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/orgs/{orgSlug}/suppliers")
            .WithTags("Suppliers")
            .RequireAuthorization();

        group.MapPost("", Create).RequireAuthorization(Permissions.WriteSuppliers).AddEndpointFilter<AuditHookFilter>();
        group.MapGet("", List);
        group.MapPatch("/{id:guid}", Patch).RequireAuthorization(Permissions.WriteSuppliers).AddEndpointFilter<AuditHookFilter>();
        group.MapDelete("/{id:guid}", Delete).RequireAuthorization(Permissions.WriteSuppliers).AddEndpointFilter<AuditHookFilter>();

        return routes;
    }

    private static async Task<Results<Created<SupplierDto>, ProblemHttpResult>> Create(
        [FromRoute] string orgSlug,
        [FromBody] CreateSupplierRequest request,
        SlugLookup slugs,
        CredVaultDbContext context,
        IUnitOfWork uow,
        IDateTimeProvider clock,
        HttpContext http,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var orgId = await slugs.FindOrganizationIdAsync(orgSlug, ct).ConfigureAwait(false);
        if (orgId is null) return ProblemDetailsHelpers.NotFound("Organisation not found.");

        try
        {
            var supplier = CredentialSupplier.Create(orgId.Value, request.SupplierType, request.DisplayName, clock);
            context.CredentialSuppliers.Add(supplier);
            await uow.SaveChangesAsync(ct).ConfigureAwait(false);

            http.Items[AuditHookFilter.OrganizationIdItem] = orgId.Value;
            http.Items[AuditHookFilter.ActionItem] = "supplier.created";
            http.Items[AuditHookFilter.TargetTypeItem] = "Supplier";
            http.Items[AuditHookFilter.TargetIdItem] = supplier.Id.ToString();

            return TypedResults.Created($"/api/v1/orgs/{orgSlug}/suppliers/{supplier.Id}", ToDto(supplier));
        }
        catch (DomainException ex)
        {
            return ProblemDetailsHelpers.BadRequest(ex.Message);
        }
    }

    private static async Task<Results<Ok<IReadOnlyList<SupplierDto>>, ProblemHttpResult>> List(
        [FromRoute] string orgSlug,
        SlugLookup slugs,
        CredVaultDbContext context,
        CancellationToken ct)
    {
        var orgId = await slugs.FindOrganizationIdAsync(orgSlug, ct).ConfigureAwait(false);
        if (orgId is null) return ProblemDetailsHelpers.NotFound("Organisation not found.");

        var rows = await context.CredentialSuppliers
            .Where(s => s.OrganizationId == orgId)
            .OrderBy(s => s.CreatedAtUtc)
            .ToListAsync(ct).ConfigureAwait(false);
        return TypedResults.Ok<IReadOnlyList<SupplierDto>>(rows.Select(ToDto).ToList());
    }

    private static async Task<Results<Ok<SupplierDto>, ProblemHttpResult>> Patch(
        [FromRoute] string orgSlug,
        [FromRoute] Guid id,
        [FromBody] UpdateSupplierRequest request,
        SlugLookup slugs,
        CredVaultDbContext context,
        IUnitOfWork uow,
        HttpContext http,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var orgId = await slugs.FindOrganizationIdAsync(orgSlug, ct).ConfigureAwait(false);
        if (orgId is null) return ProblemDetailsHelpers.NotFound("Organisation not found.");

        var supplier = await context.CredentialSuppliers
            .FirstOrDefaultAsync(s => s.OrganizationId == orgId && s.Id == id, ct).ConfigureAwait(false);
        if (supplier is null) return ProblemDetailsHelpers.NotFound("Supplier not found.");

        try
        {
            if (request.DisplayName is not null) supplier.Rename(request.DisplayName);
            if (request.IsActive == true) supplier.Activate();
            else if (request.IsActive == false) supplier.Deactivate();

            await uow.SaveChangesAsync(ct).ConfigureAwait(false);

            http.Items[AuditHookFilter.OrganizationIdItem] = orgId.Value;
            http.Items[AuditHookFilter.ActionItem] = "supplier.updated";
            http.Items[AuditHookFilter.TargetTypeItem] = "Supplier";
            http.Items[AuditHookFilter.TargetIdItem] = supplier.Id.ToString();

            return TypedResults.Ok(ToDto(supplier));
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

        var supplier = await context.CredentialSuppliers
            .FirstOrDefaultAsync(s => s.OrganizationId == orgId && s.Id == id, ct).ConfigureAwait(false);
        if (supplier is null) return ProblemDetailsHelpers.NotFound("Supplier not found.");

        var hasActive = await context.Credentials
            .AnyAsync(c => c.SupplierId == supplier.Id && !c.IsRevoked, ct).ConfigureAwait(false);
        if (hasActive)
            return ProblemDetailsHelpers.Conflict("Supplier has active credentials; revoke or delete them first.");

        context.CredentialSuppliers.Remove(supplier);
        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        http.Items[AuditHookFilter.OrganizationIdItem] = orgId.Value;
        http.Items[AuditHookFilter.ActionItem] = "supplier.deleted";
        http.Items[AuditHookFilter.TargetTypeItem] = "Supplier";
        http.Items[AuditHookFilter.TargetIdItem] = supplier.Id.ToString();

        return TypedResults.NoContent();
    }

    private static SupplierDto ToDto(CredentialSupplier s) =>
        new(s.Id, s.SupplierType, s.DisplayName, s.IsActive, s.CreatedAtUtc);
}
