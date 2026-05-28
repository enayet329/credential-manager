using CredVault.Api.Contracts;
using CredVault.Api.Lookups;
using CredVault.Domain.Credentials;
using CredVault.Domain.Enums;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace CredVault.Api.Endpoints;

/// <summary>Discovery endpoints for credential schemas.</summary>
public static class SchemaEndpoints
{
    public static IEndpointRouteBuilder MapSchemaEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/orgs/{orgSlug}/supplier-schemas")
            .WithTags("Schemas")
            .RequireAuthorization();

        group.MapGet("", ListSchemas)
            .WithName("ListSupplierSchemas")
            .WithSummary("List every credential schema registered for an organisation.");

        group.MapGet("/{supplierType}", GetSchema)
            .WithName("GetSupplierSchema")
            .WithSummary("Latest credential schema for a single supplier type.");

        return routes;
    }

    private static async Task<Results<Ok<IReadOnlyList<CredentialSchemaDto>>, NotFound<ProblemDetails>>> ListSchemas(
        [FromRoute] string orgSlug,
        SlugLookup slugs,
        IVaultCache cache,
        ICredentialSchemaProvider schemas,
        CancellationToken ct)
    {
        var orgId = await slugs.FindOrganizationIdAsync(orgSlug, ct).ConfigureAwait(false);
        if (orgId is null)
            return TypedResults.NotFound(new ProblemDetails { Title = "Organisation not found" });

        var key = $"schemas:org:{orgId}";
        var result = await cache.GetOrCreateAsync<IReadOnlyList<CredentialSchemaDto>>(
            key,
            _ =>
            {
                var dtos = CredentialSchemaRegistry.All
                    .Select(s => new CredentialSchemaDto(s.SupplierType, s.Version, s.Fields.Select(ToDto).ToList()))
                    .ToList();
                return ValueTask.FromResult<IReadOnlyList<CredentialSchemaDto>>(dtos);
            },
            ttl: TimeSpan.FromMinutes(5),
            tags: [$"schemas:org:{orgId}"],
            cancellationToken: ct).ConfigureAwait(false);

        return TypedResults.Ok(result);
    }

    private static async Task<Results<Ok<CredentialSchemaDto>, NotFound<ProblemDetails>>> GetSchema(
        [FromRoute] string orgSlug,
        [FromRoute] string supplierType,
        SlugLookup slugs,
        ICredentialSchemaProvider schemas,
        CancellationToken ct)
    {
        var orgId = await slugs.FindOrganizationIdAsync(orgSlug, ct).ConfigureAwait(false);
        if (orgId is null)
            return TypedResults.NotFound(new ProblemDetails { Title = "Organisation not found" });

        if (!SlugLookup.TryParseSupplier(supplierType, out var type))
            return TypedResults.NotFound(new ProblemDetails { Title = "Unknown supplier type" });

        try
        {
            var schema = schemas.GetSchema(type);
            return TypedResults.Ok(new CredentialSchemaDto(schema.SupplierType, schema.Version, schema.Fields.Select(ToDto).ToList()));
        }
        catch (DomainException)
        {
            return TypedResults.NotFound(new ProblemDetails { Title = "Schema not registered for supplier" });
        }
    }

    internal static CredentialFieldDto ToDto(CredentialField field) =>
        new(field.Key, field.DisplayName, field.FieldType, field.IsRequired, field.IsSecret,
            field.Placeholder, field.ValidationRegex, field.HelpText);
}
