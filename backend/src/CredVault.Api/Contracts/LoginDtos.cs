using System.ComponentModel.DataAnnotations;

namespace CredVault.Api.Contracts;

/// <summary>Request body for <c>POST /auth/login</c>.</summary>
public sealed record LoginRequest(
    [property: Required, EmailAddress] string Email,
    [property: Required, MinLength(1)] string Password);

/// <summary>One organisation the logged-in user belongs to.</summary>
public sealed record LoginOrganizationDto(Guid Id, string Slug, string Name, string Role);

/// <summary>Successful login response.</summary>
public sealed record LoginResponse(
    string AccessToken,
    DateTime ExpiresAtUtc,
    Guid UserId,
    string Email,
    IReadOnlyList<string> Permissions,
    IReadOnlyList<LoginOrganizationDto> Organizations);
