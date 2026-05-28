using System.ComponentModel.DataAnnotations;

namespace CredVault.Api.Contracts;

/// <summary>Request body for <c>POST /projects</c>.</summary>
public sealed record CreateProjectRequest(
    [property: Required, StringLength(100, MinimumLength = 1)] string Name,
    [property: Required, RegularExpression("^[a-z0-9-]{3,50}$")] string Slug,
    [property: StringLength(500)] string? Description);

/// <summary>Request body for <c>PATCH /projects/{slug}</c>.</summary>
public sealed record UpdateProjectRequest(
    [property: StringLength(100, MinimumLength = 1)] string? Name,
    [property: StringLength(500)] string? Description);

/// <summary>Response shape for projects.</summary>
public sealed record ProjectDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    DateTime CreatedAtUtc);
