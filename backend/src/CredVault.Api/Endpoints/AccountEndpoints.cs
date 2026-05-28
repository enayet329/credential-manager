using System.Security.Cryptography;
using System.Text;
using CredVault.Api.Auth;
using CredVault.Api.Contracts;
using CredVault.Api.Filters;
using CredVault.Domain.Abstractions;
using CredVault.Domain.Enums;
using CredVault.Domain.Organizations;
using CredVault.Domain.Users;
using CredVault.Domain.ValueObjects;
using CredVault.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CredVault.Api.Endpoints;

/// <summary>Self-service account operations — registration and password change.</summary>
public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapPost("/auth/register", Register)
            .WithTags("Auth")
            .AllowAnonymous()
            .WithMetadata(new SafetyNetAllowlistMarker())
            .WithSummary("Create an account and a personal workspace organisation.");

        routes.MapPost("/auth/password", ChangePassword)
            .WithTags("Auth")
            .RequireAuthorization()
            .WithSummary("Change the authenticated user's password.");

        routes.MapPost("/auth/forgot-password", ForgotPassword)
            .WithTags("Auth")
            .AllowAnonymous()
            .WithMetadata(new SafetyNetAllowlistMarker())
            .WithSummary("Send a password-reset email if the account exists.");

        routes.MapPost("/auth/reset-password", ResetPassword)
            .WithTags("Auth")
            .AllowAnonymous()
            .WithMetadata(new SafetyNetAllowlistMarker())
            .WithSummary("Redeem a password-reset token and set a new password.");

        return routes;
    }

    private static async Task<Results<Ok<LoginResponse>, ProblemHttpResult>> Register(
        [FromBody] RegisterRequest request,
        CredVaultDbContext db,
        AccessTokenService tokens,
        IDateTimeProvider clock,
        IUnitOfWork uow,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        Email email;
        try { email = Email.Create(request.Email); }
        catch (DomainException) { return ProblemDetailsHelpers.BadRequest("Invalid email."); }

        if (await db.Users.AnyAsync(u => u.Email == email, ct).ConfigureAwait(false))
            return ProblemDetailsHelpers.Conflict("An account with that email already exists.");

        var hash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 12);
        var user = User.Register(email, hash, clock);
        user.ConfirmEmail(); // No verification email flow yet; treat self-registered accounts as confirmed.

        // Auto-create a personal workspace org so the user lands somewhere usable.
        var workspaceName = string.IsNullOrWhiteSpace(request.WorkspaceName)
            ? $"{email.Value.Split('@')[0]}'s workspace"
            : request.WorkspaceName!;
        var slugBase = SlugifyEmail(email.Value);
        var orgSlug = await EnsureUniqueOrgSlugAsync(db, slugBase, ct).ConfigureAwait(false);

        var org = Organization.Create(workspaceName, Slug.Create(orgSlug), clock);
        org.AddMember(user.Id, OrganizationRole.Owner, clock);

        db.Users.Add(user);
        db.Organizations.Add(org);
        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        var permissions = AccessTokenService.PermissionsFor(OrganizationRole.Owner);
        var (token, expires) = tokens.Issue(user.Id, user.Email.Value, permissions);

        return TypedResults.Ok(new LoginResponse(
            AccessToken: token,
            ExpiresAtUtc: expires,
            UserId: user.Id,
            Email: user.Email.Value,
            Permissions: permissions,
            Organizations: [new LoginOrganizationDto(org.Id, org.Slug.Value, org.Name, "Owner")]));
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CredVaultDbContext db,
        ICurrentUser currentUser,
        IUnitOfWork uow,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (currentUser.ActorId == Guid.Empty)
            return ProblemDetailsHelpers.Forbidden("Authentication required.");

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == currentUser.ActorId, ct).ConfigureAwait(false);
        if (user is null) return ProblemDetailsHelpers.NotFound("User not found.");

        bool ok;
        try { ok = BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash); }
        catch (BCrypt.Net.SaltParseException) { ok = false; }
        if (!ok) return ProblemDetailsHelpers.Forbidden("Current password is incorrect.");

        var newHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword, workFactor: 12);
        // The Domain entity doesn't expose a password setter; we update via EF property API to keep
        // the change contained in the application layer. A proper domain method belongs in Phase 6.
        db.Entry(user).Property(nameof(User.PasswordHash)).CurrentValue = newHash;

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);
        return TypedResults.NoContent();
    }

    private static async Task<NoContent> ForgotPassword(
        [FromBody] ForgotPasswordRequest request,
        CredVaultDbContext db,
        IDateTimeProvider clock,
        IUnitOfWork uow,
        IEmailSender mailer,
        IOptions<FrontendOptions> frontendOptions,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Always return 204 — we never confirm whether an email is registered, because that would
        // be an account-enumeration oracle. Errors are swallowed below for the same reason.

        Email email;
        try { email = Email.Create(request.Email); }
        catch (DomainException) { return TypedResults.NoContent(); }

        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct).ConfigureAwait(false);
        if (user is null) return TypedResults.NoContent();

        var plaintextToken = GenerateResetToken();
        var hash = HashToken(plaintextToken);
        var row = PasswordResetToken.Create(user.Id, hash, clock);
        db.PasswordResetTokens.Add(row);
        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        var resetUrl = frontendOptions.Value.PasswordResetUrl(plaintextToken);
        var body =
            $"""
            Hi,

            We received a request to reset the password for your CredVault account ({email.Value}).
            Click the link below to choose a new password. The link is valid for 60 minutes and
            can only be used once.

              {resetUrl}

            If you didn't request a password reset, you can safely ignore this email — your
            password won't change unless you click the link above.
            """;

        try
        {
            await mailer.SendAsync(email.Value, "Reset your CredVault password", body, ct).ConfigureAwait(false);
        }
        catch
        {
            // Email-transport failures must not leak into the response — the token is still
            // valid and the user can try the "Send again" affordance.
        }

        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> ResetPassword(
        [FromBody] ResetPasswordRequest request,
        CredVaultDbContext db,
        IDateTimeProvider clock,
        IUnitOfWork uow,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var hash = HashToken(request.Token);

        var row = await db.PasswordResetTokens
            .FirstOrDefaultAsync(t => t.TokenHash == hash, ct)
            .ConfigureAwait(false);

        if (row is null || !row.IsRedeemable(clock))
            return ProblemDetailsHelpers.BadRequest("This reset link is invalid or has expired.");

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == row.UserId, ct).ConfigureAwait(false);
        if (user is null)
            return ProblemDetailsHelpers.BadRequest("This reset link is invalid or has expired.");

        var newHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword, workFactor: 12);
        db.Entry(user).Property(nameof(User.PasswordHash)).CurrentValue = newHash;

        row.MarkUsed(clock);

        // Invalidate any other outstanding reset tokens for this user.
        var others = await db.PasswordResetTokens
            .Where(t => t.UserId == row.UserId && t.UsedAtUtc == null && t.Id != row.Id)
            .ToListAsync(ct).ConfigureAwait(false);
        foreach (var t in others)
            t.MarkUsed(clock);

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);
        return TypedResults.NoContent();
    }

    private static string GenerateResetToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        // URL-safe base64 without padding.
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string SlugifyEmail(string email)
    {
        var local = email.Split('@')[0];
        var clean = new string([.. local.ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '-')]).Trim('-');
        while (clean.Contains("--", StringComparison.Ordinal))
            clean = clean.Replace("--", "-", StringComparison.Ordinal);
        if (clean.Length < 3) clean = clean.PadRight(3, '0');
        if (clean.Length > 40) clean = clean[..40];
        return clean;
    }

    private static async Task<string> EnsureUniqueOrgSlugAsync(CredVaultDbContext db, string baseSlug, CancellationToken ct)
    {
        var candidate = baseSlug;
        var n = 1;
        while (await db.Organizations.AnyAsync(o => o.Slug == Slug.Create(candidate), ct).ConfigureAwait(false))
        {
            n++;
            candidate = $"{baseSlug}-{n}";
            if (candidate.Length > 50) candidate = candidate[..50];
        }
        return candidate;
    }
}
