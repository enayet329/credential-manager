using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CredVault.Domain.Enums;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CredVault.Api.Auth;

/// <summary>Issues primary user JWTs (1-hour lifetime) with role-derived permission claims.</summary>
public sealed class AccessTokenService
{
    /// <summary>Lifetime of user access tokens.</summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromHours(1);

    private readonly JwtOptions _options;
    private readonly IDateTimeProvider _clock;

    public AccessTokenService(IOptions<JwtOptions> options, IDateTimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(clock);
        _options = options.Value;
        _clock = clock;
    }

    /// <summary>Maps an org role to the set of permission claims it grants.</summary>
    public static IReadOnlyList<string> PermissionsFor(OrganizationRole role) => role switch
    {
        OrganizationRole.Owner =>
        [
            Permissions.ReadMetadata, Permissions.ReadValue, Permissions.WriteCredentials,
            Permissions.WriteSuppliers, Permissions.WriteProjects, Permissions.AdminSchemas,
        ],
        OrganizationRole.Admin =>
        [
            Permissions.ReadMetadata, Permissions.ReadValue, Permissions.WriteCredentials,
            Permissions.WriteSuppliers, Permissions.WriteProjects, Permissions.AdminSchemas,
        ],
        OrganizationRole.Developer =>
        [
            Permissions.ReadMetadata, Permissions.ReadValue, Permissions.WriteCredentials,
        ],
        OrganizationRole.Viewer => [Permissions.ReadMetadata],
        _ => [],
    };

    /// <summary>Mints a user JWT carrying <paramref name="permissions"/>.</summary>
    public (string Token, DateTime ExpiresAtUtc) Issue(Guid userId, string email, IEnumerable<string> permissions)
    {
        ArgumentNullException.ThrowIfNull(permissions);
        var now = _clock.UtcNow;
        var expires = now.Add(Lifetime);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("actor_type", "user"),
        };
        foreach (var p in permissions)
            claims.Add(new Claim(AuthConstants.PermissionsClaim, p));

        var handler = new JwtSecurityTokenHandler();
        var creds = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: creds);

        return (handler.WriteToken(token), expires);
    }
}
