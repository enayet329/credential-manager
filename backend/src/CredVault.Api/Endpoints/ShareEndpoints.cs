using CredVault.Api.Auth;
using CredVault.Api.Contracts;
using CredVault.Api.Filters;
using CredVault.Api.Lookups;
using CredVault.Domain.Enums;
using CredVault.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace CredVault.Api.Endpoints;

/// <summary>Create + redeem signed share links for a single credential.</summary>
public static class ShareEndpoints
{
    public static IEndpointRouteBuilder MapShareEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapPost("/api/v1/orgs/{orgSlug}/credentials/{id:guid}/share", CreateShare)
            .WithTags("Shares")
            .RequireAuthorization(Permissions.WriteCredentials)
            .WithSummary("Issue a signed share link for a credential. Optionally email it to a recipient.");

        routes.MapGet("/shares/{token}", Redeem)
            .WithTags("Shares")
            .AllowAnonymous()
            .WithMetadata(new SafetyNetAllowlistMarker())
            .WithSummary("Public redemption endpoint. Validates the token and returns the credential according to its embedded permission.");

        return routes;
    }

    private static async Task<Results<Ok<CreateShareResponse>, ProblemHttpResult>> CreateShare(
        [FromRoute] string orgSlug,
        [FromRoute] Guid id,
        [FromBody] CreateShareRequest request,
        SlugLookup slugs,
        CredVaultDbContext db,
        ShareTokenService tokens,
        IEmailSender email,
        ICurrentUser currentUser,
        Microsoft.Extensions.Options.IOptions<FrontendOptions> frontendOptions,
        HttpContext http,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var orgId = await slugs.FindOrganizationIdAsync(orgSlug, ct).ConfigureAwait(false);
        if (orgId is null) return ProblemDetailsHelpers.NotFound("Organisation not found.");

        var cred = await db.Credentials.AsNoTracking()
            .Join(db.CredentialSuppliers.AsNoTracking(), c => c.SupplierId, s => s.Id,
                (c, s) => new { c.Id, s.OrganizationId, c.IsRevoked })
            .FirstOrDefaultAsync(x => x.Id == id && x.OrganizationId == orgId, ct).ConfigureAwait(false);
        if (cred is null) return ProblemDetailsHelpers.NotFound("Credential not found.");
        if (cred.IsRevoked) return ProblemDetailsHelpers.Conflict("Credential is revoked.");

        var lifetime = TimeSpan.FromMinutes(request.ExpiresInMinutes);
        var (token, expires) = tokens.Issue(id, orgId.Value, currentUser.ActorId, request.AllowReveal, lifetime);

        // Point the recipient at the web app, not the JSON API. The frontend page calls the API itself.
        var shareUrl = frontendOptions.Value.ShareUrl(token);

        if (!string.IsNullOrWhiteSpace(request.RecipientEmail))
        {
            var subject = $"A credential has been shared with you{(request.AllowReveal ? "" : " (metadata only)")}";
            var body = $"""
                You've been granted access to a credential on CredVault.

                Open this link before {expires:u}:
                {shareUrl}

                Permission: {(request.AllowReveal ? "view value" : "view metadata only")}
                """;
            await email.SendAsync(request.RecipientEmail!, subject, body, ct).ConfigureAwait(false);
        }

        return TypedResults.Ok(new CreateShareResponse(shareUrl, expires, request.AllowReveal));
    }

    private static async Task<Results<Ok<RedeemedShare>, ProblemHttpResult>> Redeem(
        [FromRoute] string token,
        ShareTokenService tokens,
        CredVaultDbContext db,
        ICredentialVaultService vault,
        ICurrentUser currentUser,
        HttpContext http,
        CancellationToken ct)
    {
        ShareTokenPayload payload;
        try { payload = tokens.Validate(token); }
        catch (SecurityTokenException)
        {
            return ProblemDetailsHelpers.BadRequest("Share link is invalid or expired.");
        }

        var row = await (
            from c in db.Credentials.AsNoTracking()
            join s in db.CredentialSuppliers.AsNoTracking() on c.SupplierId equals s.Id
            where c.Id == payload.CredentialId && s.OrganizationId == payload.OrganizationId
            select new { c.Id, c.Name, c.Slug, c.MaskedPreview, c.IsRevoked, s.SupplierType }
        ).FirstOrDefaultAsync(ct).ConfigureAwait(false);
        if (row is null) return ProblemDetailsHelpers.NotFound("Credential not found.");
        if (row.IsRevoked) return ProblemDetailsHelpers.Conflict("Credential is revoked.");

        IReadOnlyDictionary<string, string>? fields = null;
        if (payload.AllowReveal)
        {
            // Attribute the access to the issuing user — the redeemer is anonymous.
            var access = new CredentialAccessContext(
                ActorType.User,
                payload.IssuedByUserId == Guid.Empty ? Guid.NewGuid() : payload.IssuedByUserId,
                http.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
                http.Request.Headers.UserAgent.ToString(),
                AccessMethod.UI);

            try { fields = await vault.RetrieveAsync(payload.CredentialId, access, ct).ConfigureAwait(false); }
            catch { fields = null; }
        }

        return TypedResults.Ok(new RedeemedShare(
            CredentialId: row.Id,
            Name: row.Name,
            Slug: row.Slug.Value,
            SupplierType: row.SupplierType.ToString(),
            MaskedPreview: row.MaskedPreview,
            AllowReveal: payload.AllowReveal,
            Fields: fields));
    }
}
