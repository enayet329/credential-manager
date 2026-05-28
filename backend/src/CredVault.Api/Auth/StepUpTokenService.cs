using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CredVault.Api.Auth;

/// <summary>Issues short-lived step-up JWTs.</summary>
public sealed class StepUpTokenService
{
    private readonly JwtOptions _options;
    private readonly IDateTimeProvider _clock;

    public StepUpTokenService(IOptions<JwtOptions> options, IDateTimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(clock);
        _options = options.Value;
        _clock = clock;
    }

    /// <summary>Mints a step-up JWT for <paramref name="userId"/>. Expires after <see cref="AuthConstants.StepUpLifetime"/>.</summary>
    public (string Token, DateTime ExpiresAtUtc) Issue(Guid userId)
    {
        var now = _clock.UtcNow;
        var expires = now.Add(AuthConstants.StepUpLifetime);

        var handler = new JwtSecurityTokenHandler();
        var creds = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(AuthConstants.StepUpClaim, "true"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            ],
            notBefore: now,
            expires: expires,
            signingCredentials: creds);

        return (handler.WriteToken(token), expires);
    }
}
