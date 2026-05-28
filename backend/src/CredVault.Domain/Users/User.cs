using CredVault.Domain.Users.Events;

namespace CredVault.Domain.Users;

/// <summary>
/// A human account. Aggregate root. The user's MFA secret is never stored on this entity; only a
/// reference to its row in the encrypted-secrets table.
/// </summary>
public sealed class User : Entity
{
    /// <summary>The number of consecutive failed logins that trigger a lockout.</summary>
    public const int FailedLoginThreshold = 5;

    /// <summary>How long the account is locked once the threshold is reached.</summary>
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    /// <summary>The user's primary email address.</summary>
    public Email Email { get; private set; } = null!;

    /// <summary>An argon2/bcrypt/etc. hash of the password. Domain layer never sees the plaintext.</summary>
    public string PasswordHash { get; private set; } = string.Empty;

    /// <summary>FK into the encrypted-secrets table where the MFA seed is stored. <c>null</c> until MFA is enabled.</summary>
    public Guid? MfaSecretReferenceId { get; private set; }

    /// <summary>Whether multi-factor authentication is enabled.</summary>
    public bool MfaEnabled { get; private set; }

    /// <summary>Whether the user has clicked the email-confirmation link.</summary>
    public bool EmailConfirmed { get; private set; }

    /// <summary>UTC timestamp of the most recent successful login. <c>null</c> until first login.</summary>
    public DateTime? LastLoginUtc { get; private set; }

    /// <summary>Count of consecutive failed login attempts since the last success.</summary>
    public int FailedLoginAttempts { get; private set; }

    /// <summary>UTC timestamp at which the lockout (if any) expires.</summary>
    public DateTime? LockoutEndUtc { get; private set; }

    private User() { }

    /// <summary>Registers a new user. Emits <see cref="UserRegistered"/>.</summary>
    public static User Register(Email email, string passwordHash, IDateTimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(email);
        ArgumentNullException.ThrowIfNull(clock);

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("Password hash must not be empty.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = passwordHash,
            EmailConfirmed = false,
            MfaEnabled = false,
            FailedLoginAttempts = 0,
        };
        user.Raise(new UserRegistered(user.Id, email.Value, clock.UtcNow));
        return user;
    }

    /// <summary>Marks the email address as verified.</summary>
    public void ConfirmEmail() => EmailConfirmed = true;

    /// <summary>Enables multi-factor authentication and records the reference to the encrypted MFA secret.</summary>
    public void EnableMfa(Guid mfaSecretReferenceId, IDateTimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        if (mfaSecretReferenceId == Guid.Empty)
            throw new DomainException("MFA secret reference must not be empty.");
        if (MfaEnabled)
            throw new DomainException("MFA is already enabled for this user.");

        MfaSecretReferenceId = mfaSecretReferenceId;
        MfaEnabled = true;
        Raise(new UserMfaEnabled(Id, mfaSecretReferenceId, clock.UtcNow));
    }

    /// <summary>Disables multi-factor authentication and clears the MFA secret reference.</summary>
    public void DisableMfa()
    {
        if (!MfaEnabled)
            throw new DomainException("MFA is not enabled for this user.");
        MfaEnabled = false;
        MfaSecretReferenceId = null;
    }

    /// <summary>Resets the failure counter and records the login timestamp.</summary>
    public void RecordSuccessfulLogin(IDateTimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        LastLoginUtc = clock.UtcNow;
        FailedLoginAttempts = 0;
        LockoutEndUtc = null;
    }

    /// <summary>
    /// Increments the failure counter; once the threshold is reached the account is locked for
    /// <see cref="LockoutDuration"/>.
    /// </summary>
    public void RecordFailedLogin(IDateTimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        FailedLoginAttempts++;
        if (FailedLoginAttempts >= FailedLoginThreshold)
            LockoutEndUtc = clock.UtcNow + LockoutDuration;
    }

    /// <summary>Whether the account is currently locked at the given instant.</summary>
    public bool IsLockedOut(IDateTimeProvider clock) =>
        LockoutEndUtc is { } until && clock.UtcNow < until;
}
