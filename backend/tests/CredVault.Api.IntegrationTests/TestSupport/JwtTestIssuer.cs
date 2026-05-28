using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CredVault.Api.Auth;
using Microsoft.IdentityModel.Tokens;

namespace CredVault.Api.IntegrationTests.TestSupport;

/// <summary>Mints user JWTs (and step-up JWTs) signed with the same key as the API under test.</summary>
public static class JwtTestIssuer
{
    public const string Secret = "test-secret-with-enough-bits-for-hs256-please-thanks";
    public const string Issuer = "credvault-test";
    public const string Audience = "credvault-test";

    public static string IssueUser(Guid userId, params string[] permissions)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("actor_type", "user"),
        };
        foreach (var p in permissions)
            claims.Add(new Claim(AuthConstants.PermissionsClaim, p));

        return WriteToken(claims, TimeSpan.FromHours(1));
    }

    public static string IssueStepUp(Guid userId) =>
        WriteToken(
        [
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(AuthConstants.StepUpClaim, "true"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        ], TimeSpan.FromMinutes(5));

    private static string WriteToken(IEnumerable<Claim> claims, TimeSpan lifetime)
    {
        var handler = new JwtSecurityTokenHandler();
        var creds = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret)),
            SecurityAlgorithms.HmacSha256);

        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            notBefore: now,
            expires: now.Add(lifetime),
            signingCredentials: creds);

        return handler.WriteToken(token);
    }
}
