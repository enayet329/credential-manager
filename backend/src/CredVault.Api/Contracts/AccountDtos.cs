using System.ComponentModel.DataAnnotations;

namespace CredVault.Api.Contracts;

/// <summary>Body for <c>POST /auth/register</c>.</summary>
public sealed record RegisterRequest(
    [property: Required, EmailAddress] string Email,
    [property: Required, StringLength(128, MinimumLength = 8)] string Password,
    [property: StringLength(100, MinimumLength = 1)] string? WorkspaceName);

/// <summary>Body for <c>POST /auth/password</c>.</summary>
public sealed record ChangePasswordRequest(
    [property: Required, MinLength(1)] string CurrentPassword,
    [property: Required, StringLength(128, MinimumLength = 8)] string NewPassword);

/// <summary>Body for <c>POST /auth/forgot-password</c>.</summary>
public sealed record ForgotPasswordRequest(
    [property: Required, EmailAddress] string Email);

/// <summary>Body for <c>POST /auth/reset-password</c>.</summary>
public sealed record ResetPasswordRequest(
    [property: Required, MinLength(8)] string Token,
    [property: Required, StringLength(128, MinimumLength = 8)] string NewPassword);

/// <summary>Body for <c>POST /credentials/{id}/share</c>.</summary>
public sealed record CreateShareRequest(
    [property: Range(5, 24 * 60 * 7)] int ExpiresInMinutes = 60,
    bool AllowReveal = true,
    [property: EmailAddress] string? RecipientEmail = null);

/// <summary>Response for share creation.</summary>
public sealed record CreateShareResponse(string ShareUrl, DateTime ExpiresAtUtc, bool AllowReveal);

/// <summary>Response for a redeemed share token.</summary>
public sealed record RedeemedShare(
    Guid CredentialId,
    string Name,
    string Slug,
    string SupplierType,
    string MaskedPreview,
    bool AllowReveal,
    IReadOnlyDictionary<string, string>? Fields);
