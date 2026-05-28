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
