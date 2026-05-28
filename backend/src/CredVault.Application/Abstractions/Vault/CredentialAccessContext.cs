namespace CredVault.Application.Abstractions.Vault;

/// <summary>Actor + transport information attached to every retrieve call. Used to write the per-credential access log row.</summary>
/// <param name="ActorType">User or service-token.</param>
/// <param name="ActorId">Identity of the user or service-token row.</param>
/// <param name="IpAddress">Source IP of the request. Empty string when not known.</param>
/// <param name="UserAgent">User-agent of the caller.</param>
/// <param name="AccessMethod">UI / CLI / ServiceTokenApi.</param>
public sealed record CredentialAccessContext(
    ActorType ActorType,
    Guid ActorId,
    string IpAddress,
    string UserAgent,
    AccessMethod AccessMethod);
