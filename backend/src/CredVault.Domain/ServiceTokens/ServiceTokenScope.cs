namespace CredVault.Domain.ServiceTokens;

/// <summary>
/// One element of a service-token's scope rules. Each rule grants the token a permission on credentials
/// matching the given project/environment slugs.
/// </summary>
/// <param name="ProjectSlug">Project slug the rule applies to.</param>
/// <param name="EnvSlug">Environment slug the rule applies to.</param>
/// <param name="Permission">Permission level granted.</param>
public sealed record ServiceTokenScope(Slug ProjectSlug, Slug EnvSlug, ServiceTokenPermission Permission);
