using System.ComponentModel.DataAnnotations;

namespace CredVault.Api.Contracts;

/// <summary>Request body for <c>POST /credentials/{id}/notes</c>.</summary>
public sealed record CreateNoteRequest(
    [property: Required, StringLength(8000, MinimumLength = 1)] string Content);

/// <summary>Response with decrypted note content. Returned only to callers with <c>credentials:read:value</c>.</summary>
public sealed record CredentialNoteDto(
    Guid Id,
    Guid CredentialId,
    DateTime CreatedAtUtc,
    Guid CreatedByUserId,
    string Content);
