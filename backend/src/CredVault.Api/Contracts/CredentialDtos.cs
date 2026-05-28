using System.ComponentModel.DataAnnotations;
using CredVault.Domain.Enums;

namespace CredVault.Api.Contracts;

/// <summary>Request body for credential create.</summary>
public sealed record CreateCredentialRequest(
    [property: Required, StringLength(100, MinimumLength = 1)] string Name,
    [property: Required, RegularExpression("^[a-z0-9-]{3,50}$")] string Slug,
    DateTime? ExpiresAtUtc,
    [property: Required] IReadOnlyDictionary<string, string> Fields);

/// <summary>Request body for credential rotation.</summary>
public sealed record RotateCredentialRequest(
    [property: Required] IReadOnlyDictionary<string, string> Fields,
    DateTime? ExpiresAtUtc,
    [property: StringLength(500)] string? Reason);

/// <summary>Metadata response shape — NEVER includes field values.</summary>
public sealed record CredentialMetadataDto(
    Guid Id,
    Guid SupplierId,
    SupplierType SupplierType,
    Guid EnvironmentId,
    string Name,
    string Slug,
    string MaskedPreview,
    int CredentialSchemaVersion,
    int KekVersion,
    DateTime CreatedAtUtc,
    DateTime RotatedAtUtc,
    DateTime? ExpiresAtUtc,
    DateTime? LastAccessedAtUtc,
    long AccessCount,
    bool IsRevoked,
    DateTime? RevokedAtUtc);

/// <summary>Successful response for the credential value-retrieval endpoint.</summary>
/// <remarks>Includes the access-log entry that was created by this retrieval.</remarks>
public sealed record CredentialValueResponse(
    IReadOnlyDictionary<string, string> Fields,
    CredentialAccessDto Access);

/// <summary>Public view of a CredentialAccessLog row.</summary>
public sealed record CredentialAccessDto(
    Guid Id,
    Guid CredentialId,
    DateTime AccessedAtUtc,
    ActorType ActorType,
    Guid ActorId,
    string IpAddress,
    string UserAgent,
    AccessMethod AccessMethod,
    AccessOutcome Outcome);

/// <summary>Rotation metadata — old values are never exposed.</summary>
public sealed record CredentialRotationDto(
    Guid Id,
    Guid CredentialId,
    DateTime RotatedAtUtc,
    Guid RotatedByUserId,
    int PreviousKekVersion,
    string? Reason);

/// <summary>Cursor-paginated wrapper.</summary>
public sealed record CursorPage<T>(IReadOnlyList<T> Items, string? NextCursor);
