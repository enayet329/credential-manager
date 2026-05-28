using CredVault.Api.Auth;
using CredVault.Domain.Audit;
using Microsoft.AspNetCore.Http.HttpResults;

namespace CredVault.Api.Filters;

/// <summary>
/// Endpoint filter that writes a <see cref="AuditLog"/> row after a credential or supplier mutation
/// succeeds. Handlers attach <c>HttpContext.Items["audit.target_id"]</c> + <c>"audit.action"</c> +
/// <c>"audit.organization_id"</c> before returning; this filter reads them and persists.
/// </summary>
public sealed class AuditHookFilter : IEndpointFilter
{
    public const string TargetIdItem = "audit.target_id";
    public const string TargetTypeItem = "audit.target_type";
    public const string ActionItem = "audit.action";
    public const string OrganizationIdItem = "audit.organization_id";

    /// <inheritdoc/>
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var result = await next(context).ConfigureAwait(false);

        var http = context.HttpContext;
        var action = http.Items[ActionItem] as string;
        var targetId = http.Items[TargetIdItem] as string;
        var targetType = http.Items[TargetTypeItem] as string;
        var orgIdValue = http.Items[OrganizationIdItem];

        // Only write when the handler explicitly opted in and the response wasn't a problem.
        if (string.IsNullOrWhiteSpace(action) || string.IsNullOrWhiteSpace(targetId) || string.IsNullOrWhiteSpace(targetType))
            return result;
        if (orgIdValue is not Guid orgId || orgId == Guid.Empty)
            return result;
        if (result is ProblemHttpResult or IStatusCodeHttpResult { StatusCode: >= 400 })
            return result;

        var currentUser = http.RequestServices.GetRequiredService<ICurrentUser>();
        if (!currentUser.IsAuthenticated)
            return result;

        var repo = http.RequestServices.GetRequiredService<CredVault.Application.Abstractions.Persistence.IAuditLogRepository>();
        var clock = http.RequestServices.GetRequiredService<IDateTimeProvider>();

        var log = AuditLog.Record(
            organizationId: orgId,
            actorUserId: currentUser.ActorType == Domain.Enums.ActorType.User ? currentUser.ActorId : null,
            actorServiceTokenId: currentUser.ActorType == Domain.Enums.ActorType.ServiceToken ? currentUser.ActorId : null,
            actorIpAddress: currentUser.IpAddress,
            actorUserAgent: currentUser.UserAgent,
            action: action!,
            targetType: targetType!,
            targetId: targetId!,
            metadataJson: "{}",
            outcome: Domain.Enums.AccessOutcome.Success,
            reason: null,
            clock: clock);

        await repo.AppendAsync(log, http.RequestAborted).ConfigureAwait(false);

        return result;
    }
}
