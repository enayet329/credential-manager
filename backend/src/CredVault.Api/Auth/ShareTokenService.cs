using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CredVault.Api.Auth;

/// <summary>
/// Issues and validates short-lived JWT share tokens. Tokens are URL-safe and self-contained — no
/// DB row is needed, but they cannot be revoked individually before expiry. For revocable shares,
/// move to a DB table in Phase 6.
/// </summary>
public sealed class ShareTokenService
{
    public const string CredentialIdClaim = "cred";
    public const string OrganizationIdClaim = "org";
    public const string AllowRevealClaim = "reveal";
    public const string IssuedByClaim = "iss_by";

    private readonly JwtOptions _options;
    private readonly IDateTimeProvider _clock;

    public ShareTokenService(IOptions<JwtOptions> options, IDateTimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(clock);
        _options = options.Value;
        _clock = clock;
    }

    /// <summary>Mints a share token.</summary>
    /// <param name="credentialId">Credential the recipient can view.</param>
    /// <param name="organizationId">Owning organisation.</param>
    /// <param name="issuedByUserId">Audit attribution.</param>
    /// <param name="allowReveal">If <c>true</c>, the recipient can fetch the decrypted value; otherwise only metadata.</param>
    /// <param name="lifetime">How long before the token expires.</param>
    public (string Token, DateTime ExpiresAtUtc) Issue(
        Guid credentialId,
        Guid organizationId,
        Guid issuedByUserId,
        bool allowReveal,
        TimeSpan lifetime)
    {
        var now = _clock.UtcNow;
        var expires = now.Add(lifetime);

        var handler = new JwtSecurityTokenHandler();
        var creds = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: $"{_options.Audience}:share",
            claims:
            [
                new Claim(CredentialIdClaim, credentialId.ToString()),
                new Claim(OrganizationIdClaim, organizationId.ToString()),
                new Claim(IssuedByClaim, issuedByUserId.ToString()),
                new Claim(AllowRevealClaim, allowReveal ? "true" : "false"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            ],
            notBefore: now,
            expires: expires,
            signingCredentials: creds);

        return (handler.WriteToken(token), expires);
    }

    /// <summary>Decodes + validates a share token. Throws on tamper / expiry.</summary>
    public ShareTokenPayload Validate(string token)
    {
        ArgumentNullException.ThrowIfNull(token);
        var handler = new JwtSecurityTokenHandler();
        var principal = handler.ValidateToken(token, new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _options.Issuer,
            ValidateAudience = true,
            ValidAudience = $"{_options.Audience}:share",
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret)),
            ClockSkew = TimeSpan.Zero,
        }, out _);

        return new ShareTokenPayload(
            CredentialId: Guid.Parse(principal.FindFirstValue(CredentialIdClaim)!),
            OrganizationId: Guid.Parse(principal.FindFirstValue(OrganizationIdClaim)!),
            IssuedByUserId: Guid.Parse(principal.FindFirstValue(IssuedByClaim)!),
            AllowReveal: string.Equals(principal.FindFirstValue(AllowRevealClaim), "true", StringComparison.OrdinalIgnoreCase));
    }
}

public sealed record ShareTokenPayload(Guid CredentialId, Guid OrganizationId, Guid IssuedByUserId, bool AllowReveal);
