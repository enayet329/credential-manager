using System.Security.Cryptography;
using CredVault.Api.Contracts;
using CredVault.Api.Filters;
using CredVault.Api.Lookups;
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

/// <summary>Org membership management — invite, change role, remove.</summary>
public static class MemberEndpoints
{
    public static IEndpointRouteBuilder MapMemberEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/orgs/{orgSlug}/members")
            .WithTags("Members")
            .RequireAuthorization();

        // Listing is available to any authenticated member with read:metadata.
        group.MapGet("", List).RequireAuthorization(CredVault.Api.Auth.Permissions.ReadMetadata);

        // Mutating membership is admin-only (admin/owner roles map to admin:schemas in our
        // current model — we reuse that as a simple "is admin" gate).
        group.MapPost("", Invite)
            .RequireAuthorization(CredVault.Api.Auth.Permissions.AdminSchemas)
            .AddEndpointFilter<AuditHookFilter>();
        group.MapPatch("/{userId:guid}", UpdateRole)
            .RequireAuthorization(CredVault.Api.Auth.Permissions.AdminSchemas)
            .AddEndpointFilter<AuditHookFilter>();
        group.MapDelete("/{userId:guid}", Remove)
            .RequireAuthorization(CredVault.Api.Auth.Permissions.AdminSchemas)
            .AddEndpointFilter<AuditHookFilter>();

        return routes;
    }

    private static async Task<Results<Ok<IReadOnlyList<MemberDto>>, ProblemHttpResult>> List(
        [FromRoute] string orgSlug,
        SlugLookup slugs,
        CredVaultDbContext db,
        CancellationToken ct)
    {
        var orgId = await slugs.FindOrganizationIdAsync(orgSlug, ct).ConfigureAwait(false);
        if (orgId is null) return ProblemDetailsHelpers.NotFound("Organisation not found.");

        var rows = await (
            from m in db.OrganizationMemberships.AsNoTracking()
            join u in db.Users.AsNoTracking() on m.UserId equals u.Id
            where m.OrganizationId == orgId
            select new MemberDto(u.Id, u.Email.Value, m.Role, m.JoinedAtUtc, u.EmailConfirmed)
        ).ToListAsync(ct).ConfigureAwait(false);

        return TypedResults.Ok<IReadOnlyList<MemberDto>>(rows);
    }

    private static async Task<Results<Created<InviteMemberResponse>, ProblemHttpResult>> Invite(
        [FromRoute] string orgSlug,
        [FromBody] InviteMemberRequest request,
        SlugLookup slugs,
        CredVaultDbContext db,
        IDateTimeProvider clock,
        IUnitOfWork uow,
        CredVault.Api.Auth.IEmailSender mailer,
        HttpContext http,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var orgId = await slugs.FindOrganizationIdAsync(orgSlug, ct).ConfigureAwait(false);
        if (orgId is null) return ProblemDetailsHelpers.NotFound("Organisation not found.");

        Email email;
        try { email = Email.Create(request.Email); }
        catch (DomainException) { return ProblemDetailsHelpers.BadRequest("Invalid email."); }

        var existing = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct).ConfigureAwait(false);
        var temporaryPassword = request.InitialPassword;
        var userCreated = false;

        if (existing is null)
        {
            temporaryPassword ??= GeneratePassword();
            var hash = BCrypt.Net.BCrypt.HashPassword(temporaryPassword, workFactor: 12);
            existing = User.Register(email, hash, clock);
            existing.ConfirmEmail();
            db.Users.Add(existing);
            userCreated = true;
        }
        else
        {
            temporaryPassword = null;
        }

        var org = await db.Organizations.FirstAsync(o => o.Id == orgId, ct).ConfigureAwait(false);
        // Loading memberships explicitly so AddMember sees the full collection.
        await db.Entry(org).Collection(o => o.Memberships).LoadAsync(ct).ConfigureAwait(false);

        try
        {
            org.AddMember(existing.Id, request.Role, clock);
        }
        catch (DomainException ex)
        {
            return ProblemDetailsHelpers.Conflict(ex.Message);
        }

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        http.Items[AuditHookFilter.OrganizationIdItem] = orgId.Value;
        http.Items[AuditHookFilter.ActionItem] = "member.added";
        http.Items[AuditHookFilter.TargetTypeItem] = "OrganizationMembership";
        http.Items[AuditHookFilter.TargetIdItem] = existing.Id.ToString();

        // Email the invitee. New users get the temporary password; existing users just get a
        // notification that they were added. Failures don't abort the invite — the temp password
        // is also returned in the response so the inviter can pass it on if email is down.
        try
        {
            var subject = userCreated
                ? $"You've been added to {orgSlug} on CredVault"
                : $"You've been granted access to {orgSlug} on CredVault";
            var body = userCreated
                ? $"""
                    You've been invited to the '{orgSlug}' organisation on CredVault.

                    Sign-in details:
                      Email:    {email.Value}
                      Password: {temporaryPassword}
                      Role:     {request.Role}

                    Sign in here and change your password right away on the Account page.
                    """
                : $"""
                    You've been granted '{request.Role}' access to the '{orgSlug}' organisation on CredVault.

                    Sign in with your existing CredVault account ({email.Value}) to see it in your
                    organisation switcher.
                    """;
            await mailer.SendAsync(email.Value, subject, body, ct).ConfigureAwait(false);
        }
        catch
        {
            // Swallow — the operation succeeded and the response still carries the password when applicable.
        }

        var member = new MemberDto(existing.Id, existing.Email.Value, request.Role, clock.UtcNow, existing.EmailConfirmed);
        return TypedResults.Created(
            $"/api/v1/orgs/{orgSlug}/members/{existing.Id}",
            new InviteMemberResponse(member, userCreated, temporaryPassword));
    }

    private static async Task<Results<Ok<MemberDto>, ProblemHttpResult>> UpdateRole(
        [FromRoute] string orgSlug,
        [FromRoute] Guid userId,
        [FromBody] UpdateMemberRequest request,
        SlugLookup slugs,
        CredVaultDbContext db,
        IUnitOfWork uow,
        HttpContext http,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var orgId = await slugs.FindOrganizationIdAsync(orgSlug, ct).ConfigureAwait(false);
        if (orgId is null) return ProblemDetailsHelpers.NotFound("Organisation not found.");

        var org = await db.Organizations
            .Include(o => o.Memberships)
            .FirstOrDefaultAsync(o => o.Id == orgId, ct).ConfigureAwait(false);
        if (org is null) return ProblemDetailsHelpers.NotFound("Organisation not found.");

        try
        {
            org.ChangeMemberRole(userId, request.Role);
        }
        catch (DomainException ex)
        {
            return ProblemDetailsHelpers.NotFound(ex.Message);
        }

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        var user = await db.Users.AsNoTracking().FirstAsync(u => u.Id == userId, ct).ConfigureAwait(false);
        var membership = org.Memberships.First(m => m.UserId == userId);

        http.Items[AuditHookFilter.OrganizationIdItem] = orgId.Value;
        http.Items[AuditHookFilter.ActionItem] = "member.role_changed";
        http.Items[AuditHookFilter.TargetTypeItem] = "OrganizationMembership";
        http.Items[AuditHookFilter.TargetIdItem] = userId.ToString();

        return TypedResults.Ok(new MemberDto(user.Id, user.Email.Value, membership.Role, membership.JoinedAtUtc, user.EmailConfirmed));
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> Remove(
        [FromRoute] string orgSlug,
        [FromRoute] Guid userId,
        SlugLookup slugs,
        CredVaultDbContext db,
        IUnitOfWork uow,
        HttpContext http,
        CancellationToken ct)
    {
        var orgId = await slugs.FindOrganizationIdAsync(orgSlug, ct).ConfigureAwait(false);
        if (orgId is null) return ProblemDetailsHelpers.NotFound("Organisation not found.");

        var org = await db.Organizations
            .Include(o => o.Memberships)
            .FirstOrDefaultAsync(o => o.Id == orgId, ct).ConfigureAwait(false);
        if (org is null) return ProblemDetailsHelpers.NotFound("Organisation not found.");

        try
        {
            org.RemoveMember(userId);
        }
        catch (DomainException ex)
        {
            return ProblemDetailsHelpers.NotFound(ex.Message);
        }

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        http.Items[AuditHookFilter.OrganizationIdItem] = orgId.Value;
        http.Items[AuditHookFilter.ActionItem] = "member.removed";
        http.Items[AuditHookFilter.TargetTypeItem] = "OrganizationMembership";
        http.Items[AuditHookFilter.TargetIdItem] = userId.ToString();

        return TypedResults.NoContent();
    }

    private static string GeneratePassword()
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";
        Span<byte> bytes = stackalloc byte[16];
        RandomNumberGenerator.Fill(bytes);
        Span<char> chars = stackalloc char[16];
        for (var i = 0; i < bytes.Length; i++)
            chars[i] = alphabet[bytes[i] % alphabet.Length];
        return new string(chars);
    }
}
