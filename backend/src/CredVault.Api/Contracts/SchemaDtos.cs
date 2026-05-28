using System.ComponentModel.DataAnnotations;
using CredVault.Domain.Enums;

namespace CredVault.Api.Contracts;

/// <summary>One field in a credential schema. Mirrors <c>CredVault.Domain.Credentials.CredentialField</c>.</summary>
public sealed record CredentialFieldDto(
    string Key,
    string DisplayName,
    FieldType FieldType,
    bool IsRequired,
    bool IsSecret,
    string? Placeholder,
    string? ValidationRegex,
    string? HelpText);

/// <summary>A versioned credential schema.</summary>
public sealed record CredentialSchemaDto(
    SupplierType SupplierType,
    int Version,
    IReadOnlyList<CredentialFieldDto> Fields);

/// <summary>Admin payload for <c>POST /api/admin/credential-schemas</c>.</summary>
public sealed record RegisterSchemaRequest(
    [property: Required] SupplierType SupplierType,
    [property: Range(1, int.MaxValue)] int Version,
    [property: Required, MinLength(1)] IReadOnlyList<CredentialFieldDto> Fields);
