using CredVault.Api.Auth;
using CredVault.Api.Contracts;
using CredVault.Api.Filters;
using CredVault.Domain.Abstractions;
using CredVault.Domain.Enums;
using CredVault.Domain.ValueObjects;
using CredVault.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CredVault.Api.Endpoints;

/// <summary>Email + password login that returns a primary user JWT.</summary>
public static class LoginEndpoints
{
    public static IEndpointRouteBuilder MapLoginEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapPost("/auth/login", Login)
            .WithTags("Auth")
            .WithMetadata(new SafetyNetAllowlistMarker())
            .AllowAnonymous()
            .WithSummary("Exchange email + password for a primary user JWT.");

        return routes;
    }

    private static async Task<Results<Ok<LoginResponse>, ProblemHttpResult>> Login(
        [FromBody] LoginRequest request,
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

        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Email == email, ct)
            .ConfigureAwait(false);
        if (user is null)
            return ProblemDetailsHelpers.Forbidden("Invalid email or password.");

        if (user.IsLockedOut(clock))
            return ProblemDetailsHelpers.Forbidden("Account is locked. Try again later.");

        bool ok;
        try { ok = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash); }
        catch (BCrypt.Net.SaltParseException) { ok = false; }

        if (!ok)
        {
            user.RecordFailedLogin(clock);
            await uow.SaveChangesAsync(ct).ConfigureAwait(false);
            return ProblemDetailsHelpers.Forbidden("Invalid email or password.");
        }

        user.RecordSuccessfulLogin(clock);

        var memberships = await db.OrganizationMemberships
            .Where(m => m.UserId == user.Id)
            .Join(db.Organizations, m => m.OrganizationId, o => o.Id, (m, o) => new { o, m.Role })
            .ToListAsync(ct).ConfigureAwait(false);

        var bestRole = memberships.Count == 0
            ? OrganizationRole.Viewer
            : memberships.Select(x => x.Role).Max();
        var permissions = AccessTokenService.PermissionsFor(bestRole);

        var (token, expires) = tokens.Issue(user.Id, user.Email.Value, permissions);

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        return TypedResults.Ok(new LoginResponse(
            AccessToken: token,
            ExpiresAtUtc: expires,
            UserId: user.Id,
            Email: user.Email.Value,
            Permissions: permissions,
            Organizations: memberships
                .Select(x => new LoginOrganizationDto(x.o.Id, x.o.Slug.Value, x.o.Name, x.Role.ToString()))
                .ToList()));
    }
}
