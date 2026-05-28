using System.Text;
using CredVault.Api.Auth;
using CredVault.Api.Contracts;
using CredVault.Api.Filters;
using CredVault.Api.Lookups;
using CredVault.Domain.Abstractions;
using CredVault.Domain.Credentials;
using CredVault.Infrastructure.Persistence;
using CredVault.Infrastructure.Vault;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DomainCredentialEnvelope = CredVault.Domain.Credentials.CredentialEnvelope;

namespace CredVault.Api.Endpoints;

/// <summary>Encrypted runbook-style notes attached to a credential.</summary>
public static class CredentialNoteEndpoints
{
    public static IEndpointRouteBuilder MapCredentialNoteEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/orgs/{orgSlug}/credentials/{credentialId:guid}/notes")
            .WithTags("CredentialNotes")
            .RequireAuthorization();

        group.MapPost("", Create)
            .RequireAuthorization(Permissions.WriteCredentials)
            .AddEndpointFilter<AuditHookFilter>();

        group.MapGet("", List)
            .RequireAuthorization(Permissions.ReadValue)
            .WithMetadata(new SafetyNetAllowlistMarker());

        group.MapDelete("/{noteId:guid}", Delete)
            .RequireAuthorization(Permissions.WriteCredentials)
            .AddEndpointFilter<AuditHookFilter>();

        return routes;
    }

    private static async Task<Results<Created<CredentialNoteDto>, ProblemHttpResult>> Create(
        [FromRoute] string orgSlug,
        [FromRoute] Guid credentialId,
        [FromBody] CreateNoteRequest request,
        SlugLookup slugs,
        CredVaultDbContext context,
        IEnvelopeEncryptionService encryption,
        IUnitOfWork uow,
        IDateTimeProvider clock,
        ICurrentUser currentUser,
        HttpContext http,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var orgId = await slugs.FindOrganizationIdAsync(orgSlug, ct).ConfigureAwait(false);
        if (orgId is null) return ProblemDetailsHelpers.NotFound("Organisation not found.");

        var credentialExists = await context.Credentials.AsNoTracking()
            .Join(context.CredentialSuppliers.AsNoTracking(), c => c.SupplierId, s => s.Id,
                (c, s) => new { c.Id, s.OrganizationId })
            .AnyAsync(x => x.Id == credentialId && x.OrganizationId == orgId, ct).ConfigureAwait(false);
        if (!credentialExists) return ProblemDetailsHelpers.NotFound("Credential not found.");

        var noteId = Guid.NewGuid();
        var aad = EncryptionContexts.ForCredentialNote(orgId.Value, credentialId, noteId);
        var payload = await encryption.EncryptAsync(Encoding.UTF8.GetBytes(request.Content), aad, ct).ConfigureAwait(false);

        var envelope = new DomainCredentialEnvelope(
            payload.Ciphertext, payload.WrappedDataKey, payload.Nonce, payload.AuthTag,
            payload.KekVersion, "note");

        try
        {
            var note = CredentialNote.Create(credentialId, currentUser.ActorId, envelope, clock);
            DomainAccessors.SetCredentialNoteId(note, noteId);

            context.CredentialNotes.Add(note);
            await uow.SaveChangesAsync(ct).ConfigureAwait(false);

            http.Items[AuditHookFilter.OrganizationIdItem] = orgId.Value;
            http.Items[AuditHookFilter.ActionItem] = "credential_note.created";
            http.Items[AuditHookFilter.TargetTypeItem] = "CredentialNote";
            http.Items[AuditHookFilter.TargetIdItem] = noteId.ToString();

            return TypedResults.Created(
                $"/api/v1/orgs/{orgSlug}/credentials/{credentialId}/notes/{noteId}",
                new CredentialNoteDto(noteId, credentialId, note.CreatedAtUtc, currentUser.ActorId, request.Content));
        }
        catch (DomainException ex)
        {
            return ProblemDetailsHelpers.BadRequest(ex.Message);
        }
    }

    private static async Task<Results<Ok<IReadOnlyList<CredentialNoteDto>>, ProblemHttpResult>> List(
        [FromRoute] string orgSlug,
        [FromRoute] Guid credentialId,
        SlugLookup slugs,
        CredVaultDbContext context,
        IEnvelopeEncryptionService encryption,
        CancellationToken ct)
    {
        var orgId = await slugs.FindOrganizationIdAsync(orgSlug, ct).ConfigureAwait(false);
        if (orgId is null) return ProblemDetailsHelpers.NotFound("Organisation not found.");

        var notes = await context.CredentialNotes.AsNoTracking()
            .Where(n => n.CredentialId == credentialId)
            .OrderByDescending(n => n.CreatedAtUtc)
            .ToListAsync(ct).ConfigureAwait(false);

        var dtos = new List<CredentialNoteDto>(notes.Count);
        foreach (var n in notes)
        {
            var aad = EncryptionContexts.ForCredentialNote(orgId.Value, n.CredentialId, n.Id);
            var payload = new EncryptedPayload(n.EncryptedContent, n.WrappedDataKey, n.Nonce, n.AuthTag, n.KekVersion);
            var plaintext = await encryption.DecryptAsync(payload, aad, ct).ConfigureAwait(false);
            dtos.Add(new CredentialNoteDto(n.Id, n.CredentialId, n.CreatedAtUtc, n.CreatedByUserId, Encoding.UTF8.GetString(plaintext)));
        }

        return TypedResults.Ok<IReadOnlyList<CredentialNoteDto>>(dtos);
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> Delete(
        [FromRoute] string orgSlug,
        [FromRoute] Guid credentialId,
        [FromRoute] Guid noteId,
        SlugLookup slugs,
        CredVaultDbContext context,
        IUnitOfWork uow,
        HttpContext http,
        CancellationToken ct)
    {
        var orgId = await slugs.FindOrganizationIdAsync(orgSlug, ct).ConfigureAwait(false);
        if (orgId is null) return ProblemDetailsHelpers.NotFound("Organisation not found.");

        var note = await context.CredentialNotes
            .FirstOrDefaultAsync(n => n.Id == noteId && n.CredentialId == credentialId, ct).ConfigureAwait(false);
        if (note is null) return ProblemDetailsHelpers.NotFound("Note not found.");

        context.CredentialNotes.Remove(note);
        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        http.Items[AuditHookFilter.OrganizationIdItem] = orgId.Value;
        http.Items[AuditHookFilter.ActionItem] = "credential_note.deleted";
        http.Items[AuditHookFilter.TargetTypeItem] = "CredentialNote";
        http.Items[AuditHookFilter.TargetIdItem] = noteId.ToString();

        return TypedResults.NoContent();
    }
}
