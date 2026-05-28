using ClosedXML.Excel;
using CredVault.Api.Auth;
using CredVault.Api.Filters;
using CredVault.Api.Lookups;
using CredVault.Domain.Abstractions;
using CredVault.Domain.Enums;
using CredVault.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CredVault.Api.Endpoints;

/// <summary>Exports every credential value in an org (or a project/env subset) into an XLSX.</summary>
public static class CredentialExportEndpoint
{
    public static IEndpointRouteBuilder MapCredentialExportEndpoint(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/api/v1/orgs/{orgSlug}/credentials/export", ExportAsync)
            .WithTags("Credentials")
            .RequireAuthorization(Permissions.ReadValue)
            .WithMetadata(new SafetyNetAllowlistMarker())
            .WithSummary("Download every credential value in the org as an XLSX. Audit-logged per-row.");

        return routes;
    }

    private static async Task<IResult> ExportAsync(
        [FromRoute] string orgSlug,
        [FromQuery] string? project,
        [FromQuery] string? environment,
        SlugLookup slugs,
        CredVaultDbContext db,
        ICredentialVaultService vault,
        ICurrentUser currentUser,
        CancellationToken ct)
    {
        var orgId = await slugs.FindOrganizationIdAsync(orgSlug, ct).ConfigureAwait(false);
        if (orgId is null) return ProblemDetailsHelpers.NotFound("Organisation not found.");

        var query =
            from c in db.Credentials.AsNoTracking()
            join s in db.CredentialSuppliers.AsNoTracking() on c.SupplierId equals s.Id
            join e in db.Environments.AsNoTracking() on c.EnvironmentId equals e.Id
            join p in db.Projects.AsNoTracking() on e.ProjectId equals p.Id
            where s.OrganizationId == orgId && !c.IsRevoked
            select new { Cred = c, Supplier = s, Env = e, Project = p };

        if (!string.IsNullOrWhiteSpace(project))
        {
            var projectId = await slugs.FindProjectIdAsync(orgId.Value, project, ct).ConfigureAwait(false);
            if (projectId is not null)
                query = query.Where(x => x.Project.Id == projectId);
        }
        if (!string.IsNullOrWhiteSpace(environment))
        {
            query = query.Where(x => x.Env.Slug == CredVault.Domain.ValueObjects.Slug.Create(environment));
        }

        var rows = await query
            .OrderBy(x => x.Project.Name)
            .ThenBy(x => x.Env.Name)
            .ThenBy(x => x.Supplier.SupplierType)
            .ToListAsync(ct).ConfigureAwait(false);

        var access = new CredentialAccessContext(
            currentUser.ActorType, currentUser.ActorId, currentUser.IpAddress, currentUser.UserAgent, AccessMethod.UI);

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Credentials");

        // Header row
        var headers = new[]
        {
            "Project", "Environment", "Supplier", "Credential Name", "Slug",
            "Schema Version", "Created", "Rotated", "Expires", "Fields (JSON)",
        };
        for (var i = 0; i < headers.Length; i++)
        {
            var cell = sheet.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
        }

        var rowIndex = 2;
        foreach (var r in rows)
        {
            // Decrypt + audit-log each one. Yes, this is one decrypt per row; for very large exports
            // we'd batch — but typical workspaces have tens, not thousands, of credentials.
            IReadOnlyDictionary<string, string> fields;
            try
            {
                fields = await vault.RetrieveAsync(r.Cred.Id, access, ct).ConfigureAwait(false);
            }
            catch (DomainException)
            {
                fields = new Dictionary<string, string>();
            }

            sheet.Cell(rowIndex, 1).Value = r.Project.Name;
            sheet.Cell(rowIndex, 2).Value = r.Env.Name;
            sheet.Cell(rowIndex, 3).Value = r.Supplier.SupplierType.ToString();
            sheet.Cell(rowIndex, 4).Value = r.Cred.Name;
            sheet.Cell(rowIndex, 5).Value = r.Cred.Slug.Value;
            sheet.Cell(rowIndex, 6).Value = r.Cred.CredentialSchemaVersion;
            sheet.Cell(rowIndex, 7).Value = r.Cred.CreatedAtUtc;
            sheet.Cell(rowIndex, 8).Value = r.Cred.RotatedAtUtc;
            sheet.Cell(rowIndex, 9).Value = r.Cred.ExpiresAtUtc;
            sheet.Cell(rowIndex, 10).Value = System.Text.Json.JsonSerializer.Serialize(fields);
            rowIndex++;
        }
        sheet.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return Results.File(
            ms.ToArray(),
            contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileDownloadName: $"credvault-{orgSlug}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.xlsx");
    }
}
