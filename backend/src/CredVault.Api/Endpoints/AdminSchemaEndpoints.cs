using CredVault.Api.Auth;
using CredVault.Api.Contracts;
using CredVault.Domain.Credentials;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace CredVault.Api.Endpoints;

/// <summary>Admin endpoint for registering new schema versions without redeploying.</summary>
public static class AdminSchemaEndpoints
{
    public static IEndpointRouteBuilder MapAdminSchemaEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/admin/credential-schemas")
            .WithTags("Admin")
            .RequireAuthorization(Permissions.AdminSchemas);

        group.MapPost("", Register)
            .WithName("RegisterSchema")
            .WithSummary("Register a new credential schema version at runtime.");

        return routes;
    }

    private static async Task<Results<Created<CredentialSchemaDto>, BadRequest<ProblemDetails>>> Register(
        [FromBody] RegisterSchemaRequest request,
        ICredentialSchemaProvider schemas,
        IVaultCache cache,
        CredVault.Infrastructure.Persistence.CredVaultDbContext context,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var fields = request.Fields
            .Select(f => new CredentialField(f.Key, f.DisplayName, f.FieldType, f.IsRequired, f.IsSecret, f.Placeholder, f.ValidationRegex, f.HelpText))
            .ToList();

        try
        {
            var schema = new CredentialSchema(request.SupplierType, request.Version, fields);
            await schemas.RegisterSchemaAsync(schema, ct).ConfigureAwait(false);

            // Invalidate all organisations' cached schema lists.
            var orgIds = context.Organizations.Select(o => o.Id);
            foreach (var orgId in orgIds)
                await cache.RemoveByTagAsync($"schemas:org:{orgId}", ct).ConfigureAwait(false);

            var dto = new CredentialSchemaDto(schema.SupplierType, schema.Version, schema.Fields.Select(SchemaEndpoints.ToDto).ToList());
            return TypedResults.Created($"/api/admin/credential-schemas/{schema.SupplierType}/{schema.Version}", dto);
        }
        catch (DomainException ex)
        {
            return TypedResults.BadRequest(new ProblemDetails { Title = "Schema rejected", Detail = ex.Message });
        }
    }
}
