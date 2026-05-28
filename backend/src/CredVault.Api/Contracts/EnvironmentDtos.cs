using System.ComponentModel.DataAnnotations;
using CredVault.Domain.Enums;

namespace CredVault.Api.Contracts;

/// <summary>Request body for <c>POST /environments</c>.</summary>
public sealed record CreateEnvironmentRequest(
    [property: Required, StringLength(100, MinimumLength = 1)] string Name,
    [property: Required, RegularExpression("^[a-z0-9-]{3,50}$")] string Slug,
    [property: Required] EnvironmentType Type);

/// <summary>Request body for <c>PATCH /environments/{slug}</c>.</summary>
public sealed record UpdateEnvironmentRequest(
    [property: StringLength(100, MinimumLength = 1)] string? Name);

/// <summary>Response shape for environments.</summary>
public sealed record EnvironmentDto(
    Guid Id,
    string Name,
    string Slug,
    EnvironmentType Type,
    DateTime CreatedAtUtc);
