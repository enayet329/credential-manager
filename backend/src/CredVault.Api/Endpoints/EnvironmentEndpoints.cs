using CredVault.Api.Auth;
using CredVault.Api.Contracts;
using CredVault.Api.Filters;
using CredVault.Api.Lookups;
using CredVault.Domain.Abstractions;
using CredVault.Domain.ValueObjects;
using CredVault.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectEnv = CredVault.Domain.Projects.Environment;

namespace CredVault.Api.Endpoints;

/// <summary>Environment CRUD endpoints, scoped under a project.</summary>
public static class EnvironmentEndpoints
{
    public static IEndpointRouteBuilder MapEnvironmentEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/orgs/{orgSlug}/projects/{projectSlug}/environments")
            .WithTags("Environments")
            .RequireAuthorization();

        group.MapPost("", Create).RequireAuthorization(Permissions.WriteProjects).AddEndpointFilter<AuditHookFilter>();
        group.MapGet("", List);
        group.MapGet("/{envSlug}", Get);
        group.MapPatch("/{envSlug}", Patch).RequireAuthorization(Permissions.WriteProjects).AddEndpointFilter<AuditHookFilter>();
        group.MapDelete("/{envSlug}", Delete).RequireAuthorization(Permissions.WriteProjects).AddEndpointFilter<AuditHookFilter>();

        return routes;
    }

    private static async Task<Results<Created<EnvironmentDto>, ProblemHttpResult>> Create(
        [FromRoute] string orgSlug,
        [FromRoute] string projectSlug,
        [FromBody] CreateEnvironmentRequest request,
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
        var projectId = await slugs.FindProjectIdAsync(orgId.Value, projectSlug, ct).ConfigureAwait(false);
        if (projectId is null) return ProblemDetailsHelpers.NotFound("Project not found.");

        try
        {
            var env = ProjectEnv.Create(projectId.Value, request.Name, Slug.Create(request.Slug), request.Type, clock);
            context.Environments.Add(env);
            await uow.SaveChangesAsync(ct).ConfigureAwait(false);

            http.Items[AuditHookFilter.OrganizationIdItem] = orgId.Value;
            http.Items[AuditHookFilter.ActionItem] = "environment.created";
            http.Items[AuditHookFilter.TargetTypeItem] = "Environment";
            http.Items[AuditHookFilter.TargetIdItem] = env.Id.ToString();

            return TypedResults.Created(
                $"/api/v1/orgs/{orgSlug}/projects/{projectSlug}/environments/{env.Slug.Value}",
                ToDto(env));
        }
        catch (DomainException ex)
        {
            return ProblemDetailsHelpers.BadRequest(ex.Message);
        }
        catch (DbUpdateException)
        {
            return ProblemDetailsHelpers.Conflict("An environment with that slug already exists in this project.");
        }
    }

    private static async Task<Results<Ok<IReadOnlyList<EnvironmentDto>>, ProblemHttpResult>> List(
        [FromRoute] string orgSlug,
        [FromRoute] string projectSlug,
        SlugLookup slugs,
        CredVaultDbContext context,
        CancellationToken ct)
    {
        var orgId = await slugs.FindOrganizationIdAsync(orgSlug, ct).ConfigureAwait(false);
        if (orgId is null) return ProblemDetailsHelpers.NotFound("Organisation not found.");
        var projectId = await slugs.FindProjectIdAsync(orgId.Value, projectSlug, ct).ConfigureAwait(false);
        if (projectId is null) return ProblemDetailsHelpers.NotFound("Project not found.");

        var rows = await context.Environments
            .Where(e => e.ProjectId == projectId)
            .OrderBy(e => e.CreatedAtUtc)
            .ToListAsync(ct).ConfigureAwait(false);

        return TypedResults.Ok<IReadOnlyList<EnvironmentDto>>(rows.Select(ToDto).ToList());
    }

    private static async Task<Results<Ok<EnvironmentDto>, ProblemHttpResult>> Get(
        [FromRoute] string orgSlug,
        [FromRoute] string projectSlug,
        [FromRoute] string envSlug,
        SlugLookup slugs,
        CredVaultDbContext context,
        CancellationToken ct)
    {
        var orgId = await slugs.FindOrganizationIdAsync(orgSlug, ct).ConfigureAwait(false);
        if (orgId is null) return ProblemDetailsHelpers.NotFound("Organisation not found.");
        var projectId = await slugs.FindProjectIdAsync(orgId.Value, projectSlug, ct).ConfigureAwait(false);
        if (projectId is null) return ProblemDetailsHelpers.NotFound("Project not found.");

        var env = await context.Environments
            .FirstOrDefaultAsync(e => e.ProjectId == projectId && e.Slug == Slug.Create(envSlug), ct).ConfigureAwait(false);
        if (env is null) return ProblemDetailsHelpers.NotFound("Environment not found.");

        return TypedResults.Ok(ToDto(env));
    }

    private static async Task<Results<Ok<EnvironmentDto>, ProblemHttpResult>> Patch(
        [FromRoute] string orgSlug,
        [FromRoute] string projectSlug,
        [FromRoute] string envSlug,
        [FromBody] UpdateEnvironmentRequest request,
        SlugLookup slugs,
        CredVaultDbContext context,
        IUnitOfWork uow,
        HttpContext http,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var orgId = await slugs.FindOrganizationIdAsync(orgSlug, ct).ConfigureAwait(false);
        if (orgId is null) return ProblemDetailsHelpers.NotFound("Organisation not found.");
        var projectId = await slugs.FindProjectIdAsync(orgId.Value, projectSlug, ct).ConfigureAwait(false);
        if (projectId is null) return ProblemDetailsHelpers.NotFound("Project not found.");

        var env = await context.Environments
            .FirstOrDefaultAsync(e => e.ProjectId == projectId && e.Slug == Slug.Create(envSlug), ct).ConfigureAwait(false);
        if (env is null) return ProblemDetailsHelpers.NotFound("Environment not found.");

        try
        {
            if (request.Name is not null) env.Rename(request.Name);
            await uow.SaveChangesAsync(ct).ConfigureAwait(false);

            http.Items[AuditHookFilter.OrganizationIdItem] = orgId.Value;
            http.Items[AuditHookFilter.ActionItem] = "environment.updated";
            http.Items[AuditHookFilter.TargetTypeItem] = "Environment";
            http.Items[AuditHookFilter.TargetIdItem] = env.Id.ToString();

            return TypedResults.Ok(ToDto(env));
        }
        catch (DomainException ex)
        {
            return ProblemDetailsHelpers.BadRequest(ex.Message);
        }
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> Delete(
        [FromRoute] string orgSlug,
        [FromRoute] string projectSlug,
        [FromRoute] string envSlug,
        SlugLookup slugs,
        CredVaultDbContext context,
        IUnitOfWork uow,
        HttpContext http,
        CancellationToken ct)
    {
        var orgId = await slugs.FindOrganizationIdAsync(orgSlug, ct).ConfigureAwait(false);
        if (orgId is null) return ProblemDetailsHelpers.NotFound("Organisation not found.");
        var projectId = await slugs.FindProjectIdAsync(orgId.Value, projectSlug, ct).ConfigureAwait(false);
        if (projectId is null) return ProblemDetailsHelpers.NotFound("Project not found.");

        var env = await context.Environments
            .FirstOrDefaultAsync(e => e.ProjectId == projectId && e.Slug == Slug.Create(envSlug), ct).ConfigureAwait(false);
        if (env is null) return ProblemDetailsHelpers.NotFound("Environment not found.");

        context.Environments.Remove(env);
        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        http.Items[AuditHookFilter.OrganizationIdItem] = orgId.Value;
        http.Items[AuditHookFilter.ActionItem] = "environment.deleted";
        http.Items[AuditHookFilter.TargetTypeItem] = "Environment";
        http.Items[AuditHookFilter.TargetIdItem] = env.Id.ToString();

        return TypedResults.NoContent();
    }

    private static EnvironmentDto ToDto(ProjectEnv e) =>
        new(e.Id, e.Name, e.Slug.Value, e.Type, e.CreatedAtUtc);
}
