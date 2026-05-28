using CredVault.Api.Auth;
using CredVault.Api.Contracts;
using CredVault.Api.Filters;
using CredVault.Api.Lookups;
using CredVault.Domain.Abstractions;
using CredVault.Domain.Projects;
using CredVault.Domain.ValueObjects;
using CredVault.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CredVault.Api.Endpoints;

/// <summary>Project CRUD endpoints.</summary>
public static class ProjectEndpoints
{
    public static IEndpointRouteBuilder MapProjectEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/orgs/{orgSlug}/projects")
            .WithTags("Projects")
            .RequireAuthorization();

        group.MapPost("", Create)
            .RequireAuthorization(Permissions.WriteProjects)
            .AddEndpointFilter<AuditHookFilter>()
            .WithSummary("Create a project in the organisation.");

        group.MapGet("", List)
            .WithSummary("List projects in the organisation.");

        group.MapGet("/{projectSlug}", Get)
            .WithSummary("Fetch a project by slug.");

        group.MapPatch("/{projectSlug}", Patch)
            .RequireAuthorization(Permissions.WriteProjects)
            .AddEndpointFilter<AuditHookFilter>()
            .WithSummary("Update a project's name or description.");

        group.MapDelete("/{projectSlug}", Delete)
            .RequireAuthorization(Permissions.WriteProjects)
            .AddEndpointFilter<AuditHookFilter>()
            .WithSummary("Delete a project.");

        return routes;
    }

    private static async Task<Results<Created<ProjectDto>, ProblemHttpResult, ValidationProblem>> Create(
        [FromRoute] string orgSlug,
        [FromBody] CreateProjectRequest request,
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
            var project = Project.Create(orgId.Value, request.Name, Slug.Create(request.Slug), request.Description, clock);
            context.Projects.Add(project);
            await uow.SaveChangesAsync(ct).ConfigureAwait(false);

            http.Items[AuditHookFilter.OrganizationIdItem] = orgId.Value;
            http.Items[AuditHookFilter.ActionItem] = "project.created";
            http.Items[AuditHookFilter.TargetTypeItem] = "Project";
            http.Items[AuditHookFilter.TargetIdItem] = project.Id.ToString();

            return TypedResults.Created($"/api/v1/orgs/{orgSlug}/projects/{project.Slug.Value}", ToDto(project));
        }
        catch (DomainException ex)
        {
            return ProblemDetailsHelpers.BadRequest(ex.Message);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) == true)
        {
            return ProblemDetailsHelpers.Conflict("A project with that slug already exists in this organisation.");
        }
    }

    private static async Task<Results<Ok<IReadOnlyList<ProjectDto>>, ProblemHttpResult>> List(
        [FromRoute] string orgSlug,
        SlugLookup slugs,
        CredVaultDbContext context,
        CancellationToken ct)
    {
        var orgId = await slugs.FindOrganizationIdAsync(orgSlug, ct).ConfigureAwait(false);
        if (orgId is null) return ProblemDetailsHelpers.NotFound("Organisation not found.");

        var rows = await context.Projects
            .Where(p => p.OrganizationId == orgId)
            .OrderBy(p => p.CreatedAtUtc)
            .ToListAsync(ct).ConfigureAwait(false);

        return TypedResults.Ok<IReadOnlyList<ProjectDto>>(rows.Select(ToDto).ToList());
    }

    private static async Task<Results<Ok<ProjectDto>, ProblemHttpResult>> Get(
        [FromRoute] string orgSlug,
        [FromRoute] string projectSlug,
        SlugLookup slugs,
        CredVaultDbContext context,
        CancellationToken ct)
    {
        var orgId = await slugs.FindOrganizationIdAsync(orgSlug, ct).ConfigureAwait(false);
        if (orgId is null) return ProblemDetailsHelpers.NotFound("Organisation not found.");

        var project = await context.Projects
            .FirstOrDefaultAsync(p => p.OrganizationId == orgId && p.Slug == Slug.Create(projectSlug), ct).ConfigureAwait(false);
        if (project is null) return ProblemDetailsHelpers.NotFound("Project not found.");

        return TypedResults.Ok(ToDto(project));
    }

    private static async Task<Results<Ok<ProjectDto>, ProblemHttpResult>> Patch(
        [FromRoute] string orgSlug,
        [FromRoute] string projectSlug,
        [FromBody] UpdateProjectRequest request,
        SlugLookup slugs,
        CredVaultDbContext context,
        IUnitOfWork uow,
        HttpContext http,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var orgId = await slugs.FindOrganizationIdAsync(orgSlug, ct).ConfigureAwait(false);
        if (orgId is null) return ProblemDetailsHelpers.NotFound("Organisation not found.");

        var project = await context.Projects
            .FirstOrDefaultAsync(p => p.OrganizationId == orgId && p.Slug == Slug.Create(projectSlug), ct).ConfigureAwait(false);
        if (project is null) return ProblemDetailsHelpers.NotFound("Project not found.");

        try
        {
            if (request.Name is not null) project.Rename(request.Name);
            if (request.Description is not null || request is { Description: null, Name: null }) project.UpdateDescription(request.Description);

            await uow.SaveChangesAsync(ct).ConfigureAwait(false);

            http.Items[AuditHookFilter.OrganizationIdItem] = orgId.Value;
            http.Items[AuditHookFilter.ActionItem] = "project.updated";
            http.Items[AuditHookFilter.TargetTypeItem] = "Project";
            http.Items[AuditHookFilter.TargetIdItem] = project.Id.ToString();

            return TypedResults.Ok(ToDto(project));
        }
        catch (DomainException ex)
        {
            return ProblemDetailsHelpers.BadRequest(ex.Message);
        }
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> Delete(
        [FromRoute] string orgSlug,
        [FromRoute] string projectSlug,
        SlugLookup slugs,
        CredVaultDbContext context,
        IUnitOfWork uow,
        HttpContext http,
        CancellationToken ct)
    {
        var orgId = await slugs.FindOrganizationIdAsync(orgSlug, ct).ConfigureAwait(false);
        if (orgId is null) return ProblemDetailsHelpers.NotFound("Organisation not found.");

        var project = await context.Projects
            .FirstOrDefaultAsync(p => p.OrganizationId == orgId && p.Slug == Slug.Create(projectSlug), ct).ConfigureAwait(false);
        if (project is null) return ProblemDetailsHelpers.NotFound("Project not found.");

        context.Projects.Remove(project);
        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        http.Items[AuditHookFilter.OrganizationIdItem] = orgId.Value;
        http.Items[AuditHookFilter.ActionItem] = "project.deleted";
        http.Items[AuditHookFilter.TargetTypeItem] = "Project";
        http.Items[AuditHookFilter.TargetIdItem] = project.Id.ToString();

        return TypedResults.NoContent();
    }

    private static ProjectDto ToDto(Project p) => new(p.Id, p.Name, p.Slug.Value, p.Description, p.CreatedAtUtc);
}
