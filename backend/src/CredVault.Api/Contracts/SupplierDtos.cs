using System.ComponentModel.DataAnnotations;
using CredVault.Domain.Enums;

namespace CredVault.Api.Contracts;

/// <summary>Request body for <c>POST /suppliers</c>.</summary>
public sealed record CreateSupplierRequest(
    [property: Required] SupplierType SupplierType,
    [property: Required, StringLength(100, MinimumLength = 1)] string DisplayName);

/// <summary>Request body for <c>PATCH /suppliers/{id}</c>. Both fields are optional — apply the ones present.</summary>
public sealed record UpdateSupplierRequest(
    [property: StringLength(100, MinimumLength = 1)] string? DisplayName,
    bool? IsActive);

/// <summary>Response shape for suppliers.</summary>
public sealed record SupplierDto(
    Guid Id,
    SupplierType SupplierType,
    string DisplayName,
    bool IsActive,
    DateTime CreatedAtUtc);
