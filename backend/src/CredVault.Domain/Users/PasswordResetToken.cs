namespace CredVault.Domain.Users;

/// <summary>
/// Single-use token that authorises a password reset for a specific user. The plaintext token is
/// only ever in memory — the row stores a SHA-256 hash so a database compromise can't be used to
/// hijack accounts. Expiry is enforced at validation time.
/// </summary>
public sealed class PasswordResetToken : Entity
{
    /// <summary>Default lifetime for a reset token. Short, because email is the only proof of identity.</summary>
    public static readonly TimeSpan DefaultLifetime = TimeSpan.FromMinutes(60);

    /// <summary>The user this token authorises a reset for.</summary>
    public Guid UserId { get; private set; }

    /// <summary>SHA-256 hash of the token (hex, lowercase). The plaintext is never persisted.</summary>
    public string TokenHash { get; private set; } = string.Empty;

    /// <summary>UTC instant after which this token is no longer valid.</summary>
    public DateTime ExpiresAtUtc { get; private set; }

    /// <summary>UTC instant at which the token was redeemed; <c>null</c> while still pending.</summary>
    public DateTime? UsedAtUtc { get; private set; }

    /// <summary>UTC instant at which the token was minted.</summary>
    public DateTime CreatedAtUtc { get; private set; }

    private PasswordResetToken() { }

    /// <summary>Creates a fresh, unused token row.</summary>
    public static PasswordResetToken Create(Guid userId, string tokenHash, IDateTimeProvider clock, TimeSpan? lifetime = null)
    {
        ArgumentNullException.ThrowIfNull(clock);
        if (userId == Guid.Empty) throw new DomainException("UserId must not be empty.");
        if (string.IsNullOrWhiteSpace(tokenHash)) throw new DomainException("TokenHash must not be empty.");

        var now = clock.UtcNow;
        return new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
            CreatedAtUtc = now,
            ExpiresAtUtc = now.Add(lifetime ?? DefaultLifetime),
        };
    }

    /// <summary>Marks the token as redeemed.</summary>
    public void MarkUsed(IDateTimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        if (UsedAtUtc is not null) throw new DomainException("Token already used.");
        UsedAtUtc = clock.UtcNow;
    }

    /// <summary>Whether the token is still redeemable at the given instant.</summary>
    public bool IsRedeemable(IDateTimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        return UsedAtUtc is null && clock.UtcNow < ExpiresAtUtc;
    }
}
